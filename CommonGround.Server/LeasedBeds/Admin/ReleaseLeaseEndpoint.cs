using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>Ends a lease early, freeing the bed. Returns the refreshed overview plus how many are waiting.</summary>
public sealed class ReleaseLeaseEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<ReleaseLeaseEndpoint.Request, ReleaseLeaseEndpoint.Result>
{
    public sealed class Request
    {
        public int LeaseId { get; set; }
    }

    public sealed record Result(LeasedBedsOverview Overview, int WaitlistCount);

    public override void Configure()
    {
        Post("/leases/{leaseId:int}/release");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var lease = await db.BedLeases.Include(l => l.Bed).SingleOrDefaultAsync(l => l.Id == req.LeaseId, ct);
        if (lease is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (lease.Status == BedLeaseStatus.Released)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "That lease has already been released." }));
            return;
        }

        lease.Status = BedLeaseStatus.Released;
        lease.EndDate = FinancialYear.Today(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.released",
            $"released bed {lease.Bed?.Label}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);

        var waitlistCount = await db.BedRequests.CountAsync(r => r.Status == BedRequestStatus.Waitlisted, ct);
        await Send.OkAsync(new Result(await beds.GetOverviewAsync(ct), waitlistCount), ct);
    }
}
