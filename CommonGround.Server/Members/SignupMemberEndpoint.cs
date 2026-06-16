using CommonGround.Server.Activity;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Members;

public sealed class SignupMemberEndpoint(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    MembershipCheckoutService checkout,
    IOptions<ContactOptions> contactOptions,
    TurnstileVerifier turnstile,
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db,
    IActivityLogger activityLogger,
    ILogger<SignupMemberEndpoint> logger)
    : Endpoint<SignupRequest, SignupResult>
{
    private const int MaxSecondaryMembers = 4;

    public override void Configure()
    {
        Post("/membership/signup");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SignupRequest req, CancellationToken ct)
    {
        var firstName = NullIfBlank(req.FirstName);
        var lastName = NullIfBlank(req.LastName);
        var email = (req.Email ?? "").Trim();
        if (firstName is null || lastName is null || email.Length == 0 || !LooksLikeEmail(email))
        {
            await BadRequest("Please provide your name and a valid email address.");
            return;
        }
        if (string.IsNullOrWhiteSpace(req.Password))
        {
            await BadRequest("Please choose a password.");
            return;
        }

        var secondaryMembers = (req.SecondaryMembers ?? [])
            .Select(NullIfBlank)
            .OfType<string>()
            .ToList();
        if (secondaryMembers.Count > MaxSecondaryMembers)
        {
            await BadRequest($"You can list at most {MaxSecondaryMembers} additional members.");
            return;
        }

        var contact = contactOptions.Value;
        if (contact.CaptchaEnabled)
        {
            var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var ok = await turnstile.VerifyAsync(contact.TurnstileSecretKey!, req.CaptchaToken ?? "", remoteIp, ct);
            if (!ok)
            {
                await BadRequest("Captcha verification failed. Please try again.");
                return;
            }
        }

        var user = await userManager.FindByEmailAsync(email);
        // Anyone who has ever held a membership (active or lapsed) already has a real account;
        // send them to sign in rather than letting the reuse path below reset their password.
        if (user is not null && user.MembershipPaidThroughUtc is not null)
        {
            await Send.ResultAsync(Results.Conflict(new { error = "You already have a membership - please sign in." }));
            return;
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            ApplyDetails(user, firstName, lastName, req);
            var createResult = await userManager.CreateAsync(user, req.Password);
            if (!createResult.Succeeded)
            {
                await BadRequest(string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            // Existing passwordless subscriber or an abandoned pending signup:
            // upgrade the row in place and (re)set the password to the one just chosen.
            // Apply the detail changes first so the AddPassword/RemovePassword updates
            // persist them in the same write rather than needing a separate UpdateAsync.
            user.EmailConfirmed = true;
            ApplyDetails(user, firstName, lastName, req);
            if (await userManager.HasPasswordAsync(user))
            {
                await userManager.RemovePasswordAsync(user);
            }
            var addPassword = await userManager.AddPasswordAsync(user, req.Password);
            if (!addPassword.Succeeded)
            {
                await BadRequest(string.Join("; ", addPassword.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Replace secondary members.
        var existing = db.SecondaryMembers.Where(s => s.UserId == user.Id);
        db.SecondaryMembers.RemoveRange(existing);
        foreach (var name in secondaryMembers)
        {
            db.SecondaryMembers.Add(new SecondaryMember { UserId = user.Id, FullName = name });
        }

        // Payments unavailable (Stripe not configured): still accept the signup so the member
        // has an account. They sign in and pay from their profile once payments are turned on.
        if (!checkout.IsAvailable)
        {
            await db.SaveChangesAsync(ct);
            await signInManager.SignInAsync(user, isPersistent: true);
            await activityLogger.LogAsync(
                "member.signup_unpaid",
                $"{email} signed up while payments were unavailable",
                targetType: "Member",
                targetId: user.Id,
                ct: ct);
            await Send.OkAsync(new SignupResult(null), ct);
            return;
        }

        string checkoutUrl;
        try
        {
            checkoutUrl = await checkout.CreateCheckoutAsync(user, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Stripe Checkout session for {Email}", email);
            await Send.ResultAsync(Results.Problem(
                title: "Could not start payment",
                detail: "Sorry - something went wrong starting your payment. Please try again later.",
                statusCode: 502));
            return;
        }

        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "member.signup_started",
            $"{email} started membership signup",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        await Send.OkAsync(new SignupResult(checkoutUrl), ct);
    }

    private static void ApplyDetails(ApplicationUser user, string firstName, string lastName, SignupRequest req)
    {
        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = NullIfBlank(req.PhoneNumber);
        user.Address = NullIfBlank(req.Address);
        user.IsSubscribedToMailingList = req.SubscribeNewsletter;
    }

    private Task BadRequest(string error) => Send.ResultAsync(Results.BadRequest(new { error }));

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && !value.Contains(' ');
    }
}
