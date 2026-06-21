using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>Records an offline (cash/bank-transfer) payment against a lease and activates it.</summary>
public sealed class RecordLeasePaymentEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<RecordLeasePaymentEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public int LeaseId { get; set; }
    }

    public override void Configure()
    {
        Post("/leases/{leaseId:int}/record-payment");
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
            await Send.ResultAsync(Results.BadRequest(new { error = "Can't record a payment for a released lease." }));
            return;
        }

        if (await db.BedLeasePayments.AnyAsync(p => p.BedLeaseId == lease.Id && p.Status == BedLeasePaymentStatus.Paid, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "A payment has already been recorded for this lease." }));
            return;
        }

        var now = DateTime.UtcNow;
        db.BedLeasePayments.Add(new BedLeasePayment
        {
            UserId = lease.UserId,
            BedLeaseId = lease.Id,
            Method = PaymentMethod.Manual,
            StripeCheckoutSessionId = "",
            AmountCents = lease.PriceAtAllocationCents,
            Currency = "aud",
            Status = BedLeasePaymentStatus.Paid,
            CreatedAtUtc = now,
            PaidAtUtc = now,
        });

        if (lease.Status != BedLeaseStatus.Active)
        {
            lease.Status = BedLeaseStatus.Active;
        }
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.payment_recorded",
            $"recorded an offline payment for bed {lease.Bed?.Code}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
