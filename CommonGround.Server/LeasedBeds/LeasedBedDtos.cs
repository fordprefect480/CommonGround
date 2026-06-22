namespace CommonGround.Server.LeasedBeds;

/// <summary>Garden-wide bed availability. "Leased" counts in-service beds that are currently held.</summary>
public sealed record CapacitySummary(int Total, int Leased, int Remaining, bool IsFull);

/// <summary>
/// The lease currently governing a bed, for the admin allocation view.
/// <paramref name="Status"/> is the <c>BedLeaseStatus</c> name (e.g. "AwaitingPayment").
/// </summary>
public sealed record AdminBedAllocation(
    int LeaseId,
    string MemberId,
    string? MemberName,
    DateOnly StartDate,
    DateOnly ExpiresOn,
    string Status,
    int PriceAtAllocationCents,
    bool IsPaid,
    DateTime? PaidOnUtc);

public sealed record AdminBed(
    int Id,
    string Label,
    bool IsActive,
    string? Notes,
    bool IsOccupied,
    AdminBedAllocation? CurrentLease);

public sealed record LeasedBedsOverview(CapacitySummary Capacity, IReadOnlyList<AdminBed> Beds);

/// <summary>An open request in the admin queue. <paramref name="Position"/> is 1-based for waitlisted entries, null for pending.</summary>
public sealed record AdminBedRequest(
    int RequestId,
    string MemberId,
    string? MemberName,
    string? MemberEmail,
    DateTime CreatedAtUtc,
    int? Position);

public sealed record AdminBedRequests(
    IReadOnlyList<AdminBedRequest> Pending,
    IReadOnlyList<AdminBedRequest> Waitlist);

// ---- Member-facing (Profile card) ----

public sealed record MembershipInfo(bool IsActive, DateTime? PaidThroughUtc, bool CanRenew);

/// <summary><paramref name="Status"/> is the effective <c>BedLeaseStatus</c> name (Active past its expiry reads as "Expired").</summary>
public sealed record MyLease(
    int LeaseId,
    string BedLabel,
    string Status,
    int PriceAtAllocationCents,
    bool IsPaid,
    DateTime? PaidOnUtc,
    DateOnly ExpiresOn,
    bool PaymentDue,
    bool CanRenew);

/// <summary><paramref name="Status"/> is "Pending" or "Waitlisted". Position is 1-based, only set when waitlisted.</summary>
public sealed record MyRequestInfo(string Status, int? WaitlistPosition);

public sealed record MyLeasedBedStatus(
    MembershipInfo Membership,
    CapacitySummary Capacity,
    IReadOnlyList<MyLease> Leases,
    MyRequestInfo? Request);
