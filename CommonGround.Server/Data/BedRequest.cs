namespace CommonGround.Server.Data;

public enum BedRequestStatus
{
    /// <summary>Applied while beds were available; awaiting an admin to assign a bed.</summary>
    Pending = 0,

    /// <summary>Joined the waitlist because the garden was full. Position is by <see cref="BedRequest.CreatedAtUtc"/>.</summary>
    Waitlisted = 1,

    /// <summary>Resolved into a lease.</summary>
    Fulfilled = 2,

    /// <summary>Cancelled by the member.</summary>
    Withdrawn = 3,

    /// <summary>Declined / removed by an admin.</summary>
    Declined = 4,
}

/// <summary>
/// A member's in-flight request for a bed. A member may hold only one active
/// request (Pending or Waitlisted) at a time, though they may hold several leases.
/// </summary>
public class BedRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public ApplicationUser? User { get; set; }

    public BedRequestStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
}
