using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

internal static class PaymentHistory
{
    /// <summary>
    /// Returns when the member last made a paid membership payment, or null if they never have.
    /// </summary>
    public static Task<DateTime?> LastMembershipPaymentAsync(AppDbContext db, string userId, CancellationToken ct) =>
        db.MembershipPayments
            .Where(p => p.UserId == userId && p.Status == MembershipPaymentStatus.Paid)
            .MaxAsync(p => p.PaidAtUtc, ct);

    /// <summary>
    /// Loads the most recent paid membership payment date for every member who has one, keyed by user id.
    /// </summary>
    public static Task<Dictionary<string, DateTime?>> LastMembershipPaymentsAsync(AppDbContext db, CancellationToken ct) =>
        db.MembershipPayments
            .Where(p => p.Status == MembershipPaymentStatus.Paid)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, LastPaidAtUtc = g.Max(p => p.PaidAtUtc) })
            .ToDictionaryAsync(x => x.UserId, x => x.LastPaidAtUtc, ct);

    /// <summary>
    /// Loads a member's membership and leased-bed payments as one list, newest first. Paid
    /// payments are always included; set <paramref name="includeFailed"/> to also surface failed
    /// payments (useful for admin "I paid but it didn't work" support). Pending checkouts are
    /// always excluded as noise.
    /// </summary>
    public static async Task<List<PaymentDto>> LoadAsync(
        AppDbContext db, string userId, bool includeFailed, CancellationToken ct)
    {
        var membership = (await db.MembershipPayments
            .Where(p => p.UserId == userId
                && (p.Status == MembershipPaymentStatus.Paid
                    || (includeFailed && p.Status == MembershipPaymentStatus.Failed)))
            .ToListAsync(ct))
            .Select(p => p.ToDto());

        // Beds are soft-deleted, and the label is read through the Bed navigation. Ignore the
        // soft-delete query filter here so a since-deleted bed doesn't drop a real payment from
        // the member's history (historical payments must survive bed deletion).
        var beds = (await db.BedLeasePayments
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId
                && (p.Status == BedLeasePaymentStatus.Paid
                    || (includeFailed && p.Status == BedLeasePaymentStatus.Failed)))
            .Select(p => new { Payment = p, BedLabel = p.BedLease!.Bed!.Label })
            .ToListAsync(ct))
            .Select(x => x.Payment.ToDto(x.BedLabel));

        return membership.Concat(beds)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();
    }
}
