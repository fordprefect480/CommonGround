using System.ComponentModel.DataAnnotations;

namespace CommonGround.Server.Data;

public enum BedLeasePaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
}

/// <summary>
/// A payment against a <see cref="BedLease"/>. Mirrors <see cref="MembershipPayment"/>
/// but references the lease (which carries the term) instead of period dates, and
/// supports offline (<see cref="PaymentMethod.Manual"/>) payments recorded by an admin.
/// </summary>
public class BedLeasePayment
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    public int BedLeaseId { get; set; }

    public BedLease? BedLease { get; set; }

    public PaymentMethod Method { get; set; }

    /// <summary>Empty for <see cref="PaymentMethod.Manual"/> payments.</summary>
    [MaxLength(255)]
    public string StripeCheckoutSessionId { get; set; } = "";

    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public int AmountCents { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "aud";

    public BedLeasePaymentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }
}
