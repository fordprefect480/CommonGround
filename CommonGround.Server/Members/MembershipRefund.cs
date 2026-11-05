using CommonGround.Server.Data;

namespace CommonGround.Server.Members;

/// <summary>
/// Works out a member's paid-through date after a membership payment is refunded. Only a full
/// refund can change it, and only when the current paid-through came from that payment, so an
/// accidental double payment (another payment still covers the year) and hand-set or imported
/// dates are left alone.
/// </summary>
public static class MembershipRefund
{
    public static DateTime? RecalculatePaidThrough(
        DateTime? currentPaidThroughUtc, MembershipPayment refunded, IEnumerable<MembershipPayment> otherPayments)
    {
        if (!IsFullyRefunded(refunded) || refunded.PeriodEndUtc is null || currentPaidThroughUtc != refunded.PeriodEndUtc)
        {
            return currentPaidThroughUtc;
        }

        var stillCovered = otherPayments
            .Where(p => p.Id != refunded.Id
                && p.Status == MembershipPaymentStatus.Paid
                && !IsFullyRefunded(p)
                && p.PeriodEndUtc is not null)
            .Select(p => p.PeriodEndUtc)
            .Max();

        return stillCovered ?? CoverageBefore(refunded);
    }

    // A $0 (comped) payment can't be refunded, so it always keeps covering its year.
    private static bool IsFullyRefunded(MembershipPayment p) => p.AmountCents > 0 && p.RefundedAmountCents >= p.AmountCents;

    // A mid-year payment starts after the year it covers began, so falling back to PeriodStartUtc
    // would still read as paid for that year; roll back to the start of the covered year instead.
    private static DateTime? CoverageBefore(MembershipPayment refunded)
    {
        var yearStart = refunded.PeriodEndUtc!.Value.AddYears(-1);
        return refunded.PeriodStartUtc is { } start && start < yearStart ? start : yearStart;
    }
}
