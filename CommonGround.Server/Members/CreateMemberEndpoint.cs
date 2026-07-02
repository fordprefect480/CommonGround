using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Members;

public sealed class CreateMemberEndpoint(
    UserManager<ApplicationUser> userManager,
    IActivityLogger activityLogger,
    TransactionalEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    IOptions<GardenOptions> gardenOptions)
    : Endpoint<CreateMemberDto, MemberDto>
{
    public override void Configure()
    {
        Post("/members");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CreateMemberDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
        {
            await SendBadRequest("Email is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(req.Password))
        {
            await SendBadRequest("Password is required.");
            return;
        }

        var email = req.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = MemberHelpers.NullIfBlank(req.FirstName),
            LastName = MemberHelpers.NullIfBlank(req.LastName),
            PhoneNumber = MemberHelpers.NullIfBlank(req.PhoneNumber),
            IsSubscribedToMailingList = req.IsSubscribedToMailingList,
        };

        var createResult = await userManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            await SendBadRequest(string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roles = new List<string>();
        if (req.IsAdmin)
        {
            var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Admin);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                await SendBadRequest(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                return;
            }
            roles.Add(AppRoles.Admin);
        }

        await activityLogger.LogAsync(
            "member.created",
            $"added a new member, {user.Email}{(req.IsAdmin ? " (admin)" : "")}",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        await SendAccountCreatedEmailAsync(user, ct);

        var dto = new MemberDto(
            user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.Address, user.JoinedAt, user.MembershipPaidThroughUtc,
            user.EmailConfirmed, user.IsSubscribedToMailingList, [], roles.ToArray());

        await Send.ResultAsync(Results.Created($"/api/admin/members/{user.Id}", dto));
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));

    private async Task SendAccountCreatedEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        if (!emailOptions.Value.IsConfigured || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var profileUrl = string.IsNullOrWhiteSpace(gardenOptions.Value.PublicUrl)
            ? null
            : $"{gardenOptions.Value.PublicUrl.TrimEnd('/')}/profile";

        await emailSender.SendAsync(
            MemberAccountCreatedEmail.Subject,
            MemberAccountCreatedEmail.BuildHtml(user.FirstName, profileUrl),
            MemberAccountCreatedEmail.BuildText(user.FirstName, profileUrl),
            new TransactionalEmailSender.Recipient(user.Email, user.Id),
            ct: ct);
    }
}
