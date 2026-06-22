using CommonGround.Server.Activity;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.LeasedBeds.Member;

/// <summary>Member cancels their pending application or leaves the waiting list.</summary>
public sealed class WithdrawBedRequestEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : EndpointWithoutRequest<MyLeasedBedStatus>
{
    public override void Configure()
    {
        Post("/leased-beds/requests/withdraw");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var request = await beds.GetActiveRequestAsync(user.Id, ct);
        if (request is not null)
        {
            request.Status = BedRequestStatus.Withdrawn;
            request.ResolvedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await activityLogger.LogAsync(
                "leased_bed.request_withdrawn",
                "withdrew their bed request",
                targetType: "Member",
                targetId: user.Id,
                ct: ct);
        }

        await Send.OkAsync(await beds.GetMyStatusAsync(user, ct), ct);
    }
}
