using System.ComponentModel.DataAnnotations;

namespace CommonGround.Server.Data;

public enum BedLeaseStatus
{
    /// <summary>Assigned with a fee &gt; $0 that has not been paid yet; the bed is held.</summary>
    AwaitingPayment = 0,

    /// <summary>Paid (or allocated free); the lease is in force for its financial year.</summary>
    Active = 1,

    /// <summary>Past <see cref="BedLease.ExpiresOn"/> and not renewed. Still occupies the bed until released.</summary>
    Expired = 2,

    /// <summary>Ended by an admin; the bed is free again.</summary>
    Released = 3,
}

/// <summary>
/// One member's right to one <see cref="Bed"/> for one financial year, with its
/// own snapshotted fee and payment(s). Renewing creates a new lease for the next
/// year rather than mutating this one, giving clean per-year history.
/// </summary>
public class BedLease
{
    public int Id { get; set; }

    public int BedId { get; set; }

    public Bed? Bed { get; set; }

    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>End of the financial year for this term (30 June). Stored, not recomputed on read.</summary>
    public DateOnly ExpiresOn { get; set; }

    /// <summary>Set only when an admin releases the lease early.</summary>
    public DateOnly? EndDate { get; set; }

    public BedLeaseStatus Status { get; set; }

    /// <summary>The fee snapshotted at allocation, in cents. Admin-set; may be 0.</summary>
    public int PriceAtAllocationCents { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<BedLeasePayment> Payments { get; set; } = new List<BedLeasePayment>();
}
