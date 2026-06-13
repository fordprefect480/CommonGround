using CommonGround.Server.Activity;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Email;

public sealed class SubscribeEndpoint(
    UserManager<ApplicationUser> userManager,
    IOptions<ContactOptions> contactOptions,
    TurnstileVerifier turnstile,
    IHttpContextAccessor httpContextAccessor,
    IActivityLogger activityLogger)
    : Endpoint<SubscribeEndpoint.Request, SubscribeEndpoint.Result>
{
    public sealed record Request(string Email, string? CaptchaToken);
    public sealed record Result(bool Subscribed);

    private const int MaxEmailLength = 256;

    public override void Configure()
    {
        Post("/subscribe");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim();
        if (email.Length == 0 || email.Length > MaxEmailLength || !LooksLikeEmail(email))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Please enter a valid email address." }));
            return;
        }

        // The Turnstile keys live on ContactOptions but the captcha is site-wide.
        var contact = contactOptions.Value;
        if (contact.CaptchaEnabled)
        {
            var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var ok = await turnstile.VerifyAsync(contact.TurnstileSecretKey!, req.CaptchaToken ?? "", remoteIp, ct);
            if (!ok)
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "Captcha verification failed. Please try again." }));
                return;
            }
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!existing.IsSubscribedToMailingList)
            {
                existing.IsSubscribedToMailingList = true;
                await userManager.UpdateAsync(existing);
                await activityLogger.LogAsync(
                    "mailinglist.subscribed",
                    $"{email} re-subscribed to the mailing list",
                    targetType: "Member",
                    targetId: existing.Id,
                    ct: ct);
            }

            // Idempotent: don't reveal whether the address was already registered.
            await Send.OkAsync(new Result(true), ct);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            IsSubscribedToMailingList = true,
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Could not subscribe that email address. Please try again." }));
            return;
        }

        await activityLogger.LogAsync(
            "mailinglist.subscribed",
            $"{email} subscribed to the mailing list",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        await Send.OkAsync(new Result(true), ct);
    }

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && !value.Contains(' ');
    }
}
