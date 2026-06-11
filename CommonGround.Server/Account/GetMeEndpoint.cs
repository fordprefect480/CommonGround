using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Account;

public sealed class GetMeEndpoint(UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<MeDto>
{
    public override void Configure()
    {
        Get("/account/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var isAdmin = await userManager.IsInRoleAsync(current, AppRoles.Admin);

        await Send.OkAsync(new MeDto(
            current.Email,
            current.FirstName,
            current.LastName,
            current.DisplayName,
            isAdmin,
            current.IsSubscribedToMailingList), ct);
    }
}
