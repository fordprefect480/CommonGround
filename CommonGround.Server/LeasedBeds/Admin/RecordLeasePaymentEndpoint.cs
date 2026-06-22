using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

/// <summary>Records a manual (cash/bank-transfer) payment of the given amount against a lease and activates it.</summary>
public sealed class RecordLeasePaymentEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<RecordLeasePaymentEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public int LeaseId { get; set; }

        /// <summary>The amount actually received, in cents. Set by the admin in the record-payment dialog.</summary>
        public int AmountCents { get; set; }
    }

    public override void Configure()
    {
        Post("/leases/{leaseId:int}/record-payment");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (req.AmountCents < 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Enter an amount of $0 or more." }));
            return;
        }

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
            AmountCents = req.AmountCents,
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
            $"recorded an offline payment for bed {lease.Bed?.Label}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
