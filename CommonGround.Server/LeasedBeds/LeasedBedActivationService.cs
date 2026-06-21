using CommonGround.Server.Activity;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Confirms a pending leased-bed payment exactly once. Invoked by the Stripe webhook.
/// The pending->paid transition is an atomic conditional update so concurrent webhook
/// retries cannot double-activate.
/// </summary>
public sealed class LeasedBedActivationService(
    AppDbContext db,
    IActivityLogger activityLogger,
    ILogger<LeasedBedActivationService> logger)
{
    /// <summary>Marks the payment for the session as paid and activates its lease. Safe to call repeatedly.</summary>
    public async Task ActivateAsync(string checkoutSessionId, string? paymentIntentId, CancellationToken ct)
    {
        var payment = await db.BedLeasePayments
            .SingleOrDefaultAsync(p => p.StripeCheckoutSessionId == checkoutSessionId, ct);
        if (payment is null)
        {
            logger.LogWarning("No bed lease payment found for checkout session {SessionId}", checkoutSessionId);
            return;
        }

        if (payment.Status == BedLeasePaymentStatus.Paid)
        {
            return; // already processed
        }

        var now = DateTime.UtcNow;
        var rows = await db.BedLeasePayments
            .Where(p => p.Id == payment.Id && p.Status == BedLeasePaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, BedLeasePaymentStatus.Paid)
                .SetProperty(p => p.PaidAtUtc, now)
                .SetProperty(p => p.StripePaymentIntentId, paymentIntentId), ct);

        if (rows == 0)
        {
            return; // another caller won the race
        }

        var lease = await db.BedLeases
            .Include(l => l.Bed)
            .Include(l => l.User)
            .SingleAsync(l => l.Id == payment.BedLeaseId, ct);

        if (lease.Status == BedLeaseStatus.Released)
        {
            // The bed was released between checkout and this webhook. The member was charged,
            // so keep the payment recorded but don't resurrect the lease - flag for admin/refund.
            logger.LogWarning(
                "Bed lease {LeaseId} was released before payment {PaymentId} confirmed; recorded but not reactivated.",
                lease.Id, payment.Id);
            await activityLogger.LogAsync(
                "leased_bed.payment_orphaned",
                $"Payment received for released bed {lease.Bed?.Code} (lease {lease.Id}) - needs review.",
                targetType: "BedLease",
                targetId: lease.Id.ToString(),
                ct: ct);
            return;
        }

        if (lease.Status != BedLeaseStatus.Active)
        {
            lease.Status = BedLeaseStatus.Active;
            await db.SaveChangesAsync(ct);
        }

        await activityLogger.LogAsync(
            "leased_bed.paid",
            $"{lease.User?.Email} paid for bed {lease.Bed?.Code}",
            targetType: "BedLease",
            targetId: lease.Id.ToString(),
            ct: ct);
    }

    /// <summary>Marks a still-pending payment as failed when its Checkout session expires.</summary>
    public async Task ExpireAsync(string checkoutSessionId, CancellationToken ct)
    {
        await db.BedLeasePayments
            .Where(p => p.StripeCheckoutSessionId == checkoutSessionId && p.Status == BedLeasePaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, BedLeasePaymentStatus.Failed), ct);
    }
}
