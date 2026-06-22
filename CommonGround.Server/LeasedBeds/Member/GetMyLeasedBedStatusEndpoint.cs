using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.LeasedBeds.Member;

public sealed class GetMyLeasedBedStatusEndpoint(
    UserManager<ApplicationUser> userManager,
    LeasedBedService beds)
    : EndpointWithoutRequest<MyLeasedBedStatus>
{
    public override void Configure()
    {
        Get("/leased-beds/my-status");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await beds.GetMyStatusAsync(user, ct), ct);
    }
}
