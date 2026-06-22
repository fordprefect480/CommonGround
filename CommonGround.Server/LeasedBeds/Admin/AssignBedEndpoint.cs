using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>
/// Assigns a specific bed to a member, snapshotting the fee.
/// The member can be identified either by an existing bed request (<see cref="Request.RequestId"/>),
/// which is then marked fulfilled, or directly by <see cref="Request.UserId"/> for a member who never
/// applied. A $0 fee activates the lease immediately; otherwise it is held AwaitingPayment.
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
        /// <summary>Fulfils an existing bed request. Provide exactly one of <see cref="RequestId"/> or <see cref="UserId"/>.</summary>
        public int? RequestId { get; set; }

        /// <summary>Assigns directly to an existing member who hasn't applied. Provide exactly one of <see cref="RequestId"/> or <see cref="UserId"/>.</summary>
        public string? UserId { get; set; }

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
        // Resolve who the bed is for. Either an existing request (which we then fulfil) or a member id.
        BedRequest? request = null;
        ApplicationUser? user;

        if (req.RequestId is { } requestId)
        {
            request = await db.BedRequests.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == requestId, ct);
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

            user = request.User;
        }
        else if (!string.IsNullOrWhiteSpace(req.UserId))
        {
            user = await db.Users.SingleOrDefaultAsync(u => u.Id == req.UserId, ct);
            if (user is null)
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "That member no longer exists." }));
                return;
            }

            // If the member happens to have an in-flight request, fulfil it too so they don't linger on the list.
            request = await beds.GetActiveRequestAsync(user.Id, ct);
        }
        else
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Choose a member to assign the bed to." }));
            return;
        }

        // A member may only hold one bed at a time. The request flow can't reach here for an
        // existing holder (they couldn't have an open request), but the direct UserId path can,
        // and there's no DB constraint, so guard against giving someone a second overlapping lease.
        var targetUserId = request?.UserId ?? user!.Id;
        if (await db.BedLeases.AnyAsync(l => l.UserId == targetUserId && l.Status != BedLeaseStatus.Released, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "That member already holds a leased bed." }));
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
            await Send.ResultAsync(Results.BadRequest(new { error = $"Bed {bed.Label} is out of service." }));
            return;
        }

        // Re-check availability to guard the page-load/click race and two admins assigning at once.
        if (await beds.IsBedOccupiedAsync(bed.Id, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"Bed {bed.Label} is already leased." }));
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
            UserId = targetUserId,
            StartDate = today,
            ExpiresOn = FinancialYear.GetFinancialYearEnd(today),
            Status = price == 0 ? BedLeaseStatus.Active : BedLeaseStatus.AwaitingPayment,
            PriceAtAllocationCents = price,
            CreatedAtUtc = now,
        };
        db.BedLeases.Add(lease);

        if (request is not null)
        {
            request.Status = BedRequestStatus.Fulfilled;
            request.ResolvedAtUtc = now;
        }
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.assigned",
            $"assigned bed {bed.Label} to {user?.DisplayName ?? user?.Email ?? lease.UserId}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);

        if (user is not null)
        {
            await notifications.SendAssignmentAsync(user, bed.Label, lease.ExpiresOn, price, ct);
        }

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
