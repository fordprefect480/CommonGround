using CommonGround.Server.Data;
using CommonGround.Server.Members;

namespace CommonGround.Server.Tests;

public class MembershipRefundTests
{
    private static readonly DateTime YearStart = new(2026, 6, 30, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime YearEnd = new(2027, 6, 30, 14, 30, 0, DateTimeKind.Utc);

    private static MembershipPayment Payment(int id, int refundedCents = 0, DateTime? periodEnd = null) => new()
    {
        Id = id,
        AmountCents = 2500,
        RefundedAmountCents = refundedCents,
        Status = MembershipPaymentStatus.Paid,
        PeriodStartUtc = YearStart.AddDays(10),
        PeriodEndUtc = periodEnd ?? YearEnd,
    };

    [Fact]
    public void Partial_refund_keeps_paid_through()
    {
        var refunded = Payment(1, refundedCents: 1000);

        var result = MembershipRefund.RecalculatePaidThrough(YearEnd, refunded, [refunded]);

        Assert.Equal(YearEnd, result);
    }

    [Fact]
    public void Full_refund_of_a_duplicate_payment_keeps_paid_through()
    {
        var original = Payment(1);
        var duplicate = Payment(2, refundedCents: 2500);

        var result = MembershipRefund.RecalculatePaidThrough(YearEnd, duplicate, [original, duplicate]);

        Assert.Equal(YearEnd, result);
    }

    [Fact]
    public void Full_refund_of_the_only_payment_rolls_back_to_the_start_of_the_covered_year()
    {
        var refunded = Payment(1, refundedCents: 2500);

        var result = MembershipRefund.RecalculatePaidThrough(YearEnd, refunded, [refunded]);

        Assert.Equal(YearStart, result);
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(result, refunded.PeriodStartUtc!.Value));
    }

    [Fact]
    public void Full_refund_falls_back_to_the_latest_remaining_payment()
    {
        var lastYear = Payment(1, periodEnd: YearStart);
        var refunded = Payment(2, refundedCents: 2500);

        var result = MembershipRefund.RecalculatePaidThrough(YearEnd, refunded, [lastYear, refunded]);

        Assert.Equal(YearStart, result);
    }

    [Fact]
    public void Full_refund_ignores_other_payments_that_were_also_fully_refunded()
    {
        var alsoRefunded = Payment(1, refundedCents: 2500);
        var refunded = Payment(2, refundedCents: 2500);

        var result = MembershipRefund.RecalculatePaidThrough(YearEnd, refunded, [alsoRefunded, refunded]);

        Assert.Equal(YearStart, result);
    }

    [Fact]
    public void Paid_through_not_set_by_the_refunded_payment_is_left_alone()
    {
        var handSet = YearEnd.AddYears(1);
        var refunded = Payment(1, refundedCents: 2500);

        var result = MembershipRefund.RecalculatePaidThrough(handSet, refunded, [refunded]);

        Assert.Equal(handSet, result);
    }
}
