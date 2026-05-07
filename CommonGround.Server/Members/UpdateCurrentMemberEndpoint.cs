using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Members;

public sealed class UpdateCurrentMemberEndpoint(UserManager<ApplicationUser> userManager)
    : Endpoint<UpdateProfileDto, AdminMeDto>
{
    public override void Configure()
    {
        Put("/me");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(UpdateProfileDto req, CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        current.FirstName = MemberHelpers.NullIfBlank(req.FirstName);
        current.LastName = MemberHelpers.NullIfBlank(req.LastName);

        var result = await userManager.UpdateAsync(current);
        if (!result.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = string.Join("; ", result.Errors.Select(e => e.Description)),
            }));
            return;
        }

        await Send.OkAsync(new AdminMeDto(
            User.Identity?.Name,
            current.FirstName,
            current.LastName,
            current.DisplayName,
            true), ct);
    }
}
