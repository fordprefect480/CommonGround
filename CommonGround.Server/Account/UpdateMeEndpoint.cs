using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Account;

public sealed class UpdateMeEndpoint(
    UserManager<ApplicationUser> userManager,
    IActivityLogger activityLogger)
    : Endpoint<UpdateProfileDto, MeDto>
{
    public override void Configure()
    {
        Put("/account/me");
    }

    public override async Task HandleAsync(UpdateProfileDto req, CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        current.FirstName = NullIfBlank(req.FirstName);
        current.LastName = NullIfBlank(req.LastName);
        current.IsSubscribedToMailingList = req.IsSubscribedToMailingList;

        var result = await userManager.UpdateAsync(current);
        if (!result.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = string.Join("; ", result.Errors.Select(e => e.Description)),
            }));
            return;
        }

        await activityLogger.LogAsync(
            "member.profile_updated",
            "Updated own profile",
            targetType: "Member",
            targetId: current.Id,
            ct: ct);

        var isAdmin = await userManager.IsInRoleAsync(current, AppRoles.Admin);

        await Send.OkAsync(new MeDto(
            current.Email,
            current.FirstName,
            current.LastName,
            current.DisplayName,
            isAdmin,
            current.IsSubscribedToMailingList), ct);
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
