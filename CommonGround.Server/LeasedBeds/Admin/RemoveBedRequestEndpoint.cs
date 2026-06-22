using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>Admin removes an open request (declines a pending application or drops a waiting-list entry).</summary>
public sealed class RemoveBedRequestEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : EndpointWithoutRequest<AdminBedRequests>
{
    public override void Configure()
    {
        Delete("/requests/{requestId:int}");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var requestId = Route<int>("requestId");
        var request = await db.BedRequests.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (request.Status is BedRequestStatus.Pending or BedRequestStatus.Waitlisted)
        {
            request.Status = BedRequestStatus.Declined;
            request.ResolvedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await activityLogger.LogAsync(
                "leased_bed.request_removed",
                $"removed {request.User?.DisplayName ?? request.User?.Email ?? request.UserId} from the bed waiting list",
                targetType: "Member",
                targetId: request.UserId,
                ct: ct);
        }

        await Send.OkAsync(await beds.GetRequestsAsync(ct), ct);
    }
}
