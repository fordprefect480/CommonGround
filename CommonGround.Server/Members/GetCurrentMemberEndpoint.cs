using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Members;

public sealed class GetCurrentMemberEndpoint(UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<AdminMeDto>
{
    public override void Configure()
    {
        Get("/me");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        await Send.OkAsync(new AdminMeDto(
            User.Identity?.Name,
            current?.FirstName,
            current?.LastName,
            current?.DisplayName,
            true), ct);
    }
}
