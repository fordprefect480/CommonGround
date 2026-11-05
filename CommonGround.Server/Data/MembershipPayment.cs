using System.ComponentModel.DataAnnotations;

namespace CommonGround.Server.Data;

public enum MembershipPaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
}

public class MembershipPayment
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    public PaymentMethod Method { get; set; }

    [MaxLength(255)]
    public string StripeCheckoutSessionId { get; set; } = "";

    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public int AmountCents { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "aud";

    public MembershipPaymentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Total refunded so far, in cents. Refunds are made in Stripe (or in cash) and recorded here by an admin.</summary>
    public int RefundedAmountCents { get; set; }

    /// <summary>When the most recent refund was recorded; null if never refunded.</summary>
    public DateTime? RefundedAtUtc { get; set; }

    public DateTime? PeriodStartUtc { get; set; }

    public DateTime? PeriodEndUtc { get; set; }
}
