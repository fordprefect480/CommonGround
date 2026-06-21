using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>
/// Assigns a specific bed to the member behind a request, snapshotting the fee.
/// A $0 fee activates the lease immediately; otherwise it is held AwaitingPayment.
/// </summary>
public sealed class AssignBedEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    SiteSettingsService settings,
    LeasedBedNotifications notifications,
    IActivityLogger activityLogger)
    : Endpoint<AssignBedEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public int RequestId { get; set; }
        public int BedId { get; set; }

        /// <summary>Overrides the standard price when set. Must be ≥ 0.</summary>
        public int? CustomPriceCents { get; set; }
    }

    public override void Configure()
    {
        Post("/assign");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var request = await db.BedRequests.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == req.RequestId, ct);
        if (request is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (request.Status is not (BedRequestStatus.Pending or BedRequestStatus.Waitlisted))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "That request has already been resolved." }));
            return;
        }

        var bed = await db.Beds.SingleOrDefaultAsync(b => b.Id == req.BedId, ct);
        if (bed is null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "That bed no longer exists." }));
            return;
        }

        if (!bed.IsActive)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"Bed {bed.Code} is out of service." }));
            return;
        }

        // Re-check availability to guard the page-load/click race and two admins assigning at once.
        if (await beds.IsBedOccupiedAsync(bed.Id, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"Bed {bed.Code} is already leased." }));
            return;
        }

        var standardPrice = await settings.GetLeasedBedPriceCentsAsync(ct);
        var price = req.CustomPriceCents ?? standardPrice;
        if (!LeasedBedPrice.TryValidate(price, out var error))
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        var now = DateTime.UtcNow;
        var today = FinancialYear.Today(now);
        var lease = new BedLease
        {
            BedId = bed.Id,
            UserId = request.UserId,
            StartDate = today,
            ExpiresOn = FinancialYear.GetFinancialYearEnd(today),
            Status = price == 0 ? BedLeaseStatus.Active : BedLeaseStatus.AwaitingPayment,
            PriceAtAllocationCents = price,
            CreatedAtUtc = now,
        };
        db.BedLeases.Add(lease);

        request.Status = BedRequestStatus.Fulfilled;
        request.ResolvedAtUtc = now;
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.assigned",
            $"assigned bed {bed.Code} to {request.User?.DisplayName ?? request.User?.Email ?? request.UserId}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);

        if (request.User is not null)
        {
            await notifications.SendAssignmentAsync(request.User, bed.Code, lease.ExpiresOn, price, ct);
        }

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
