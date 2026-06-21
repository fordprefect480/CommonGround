using CommonGround.Server.Activity;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Member;

/// <summary>
/// Renews the member's lease for the next financial year at the current standard price.
/// Only valid in the renewal window (from one month before expiry onwards). $0 ⇒ Active
/// immediately, else AwaitingPayment. Idempotent: if a next-year lease already exists it is
/// left untouched.
/// </summary>
public sealed class RenewLeaseEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    LeasedBedService beds,
    SiteSettingsService settings,
    IActivityLogger activityLogger)
    : Endpoint<RenewLeaseEndpoint.Request, MyLeasedBedStatus>
{
    public sealed class Request
    {
        public int LeaseId { get; set; }
    }

    public override void Configure()
    {
        Post("/leased-beds/leases/{leaseId:int}/renew");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var lease = await db.BedLeases
            .Include(l => l.Bed)
            .SingleOrDefaultAsync(l => l.Id == req.LeaseId && l.UserId == user.Id, ct);
        if (lease is null || lease.Status == BedLeaseStatus.Released)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Only a held (paid, or free) lease can be renewed - not one still awaiting payment.
        if (lease.Status != BedLeaseStatus.Active)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "You can only renew an active lease." }));
            return;
        }

        var now = DateTime.UtcNow;
        var today = FinancialYear.Today(now);
        if (!FinancialYear.IsRenewalOpen(today, lease.ExpiresOn))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Renewals open one month before your lease expires." }));
            return;
        }

        var startDate = lease.ExpiresOn.AddDays(1);
        var expiresOn = FinancialYear.GetFinancialYearEnd(startDate);

        // Idempotent: don't create a second next-year lease for this bed.
        var alreadyRenewed = await db.BedLeases.AnyAsync(
            l => l.BedId == lease.BedId && l.UserId == user.Id && l.Status != BedLeaseStatus.Released && l.ExpiresOn == expiresOn, ct);

        if (!alreadyRenewed)
        {
            var price = await settings.GetLeasedBedPriceCentsAsync(ct);
            db.BedLeases.Add(new BedLease
            {
                BedId = lease.BedId,
                UserId = user.Id,
                StartDate = startDate,
                ExpiresOn = expiresOn,
                Status = price == 0 ? BedLeaseStatus.Active : BedLeaseStatus.AwaitingPayment,
                PriceAtAllocationCents = price,
                CreatedAtUtc = now,
            });
            await db.SaveChangesAsync(ct);

            await activityLogger.LogAsync(
                "leased_bed.renewed",
                $"renewed bed {lease.Bed?.Code} for {expiresOn.Year - 1}/{expiresOn.Year}",
                targetType: "Bed",
                targetId: lease.BedId.ToString(),
                ct: ct);
        }

        await Send.OkAsync(await beds.GetMyStatusAsync(user, ct), ct);
    }
}
