using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Shared queries for bed occupancy, capacity and the admin allocation view.
/// Occupancy has a single definition here so every consumer agrees: a bed is
/// occupied while it has any lease that has not been <see cref="BedLeaseStatus.Released"/>.
/// </summary>
public sealed class LeasedBedService(AppDbContext db)
{
    /// <summary>The bed is held while it has any non-released lease (incl. AwaitingPayment and Expired).</summary>
    public Task<bool> IsBedOccupiedAsync(int bedId, CancellationToken ct) =>
        db.BedLeases.AnyAsync(l => l.BedId == bedId && l.Status != BedLeaseStatus.Released, ct);

    public Task<bool> HasLeaseHistoryAsync(int bedId, CancellationToken ct) =>
        db.BedLeases.AnyAsync(l => l.BedId == bedId, ct);

    public async Task<CapacitySummary> GetCapacityAsync(CancellationToken ct)
    {
        var activeBedIds = await db.Beds.Where(b => b.IsActive).Select(b => b.Id).ToListAsync(ct);
        var occupiedBedIds = await db.BedLeases
            .Where(l => l.Status != BedLeaseStatus.Released)
            .Select(l => l.BedId)
            .Distinct()
            .ToListAsync(ct);

        var occupied = occupiedBedIds.ToHashSet();
        var total = activeBedIds.Count;
        var leased = activeBedIds.Count(occupied.Contains);
        var remaining = total - leased;
        return new CapacitySummary(total, leased, remaining, remaining <= 0);
    }

    public async Task<LeasedBedsOverview> GetOverviewAsync(CancellationToken ct)
    {
        var beds = await db.Beds
            .OrderBy(b => b.Section)
            .ThenBy(b => b.Number)
            .ToListAsync(ct);

        // The lease governing each bed = its most recent non-released lease.
        var liveLeases = await db.BedLeases
            .Where(l => l.Status != BedLeaseStatus.Released)
            .Include(l => l.User)
            .Include(l => l.Payments)
            .ToListAsync(ct);

        var governingByBed = liveLeases
            .GroupBy(l => l.BedId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(l => l.ExpiresOn).ThenByDescending(l => l.CreatedAtUtc).First());

        var bedDtos = beds.Select(bed =>
        {
            governingByBed.TryGetValue(bed.Id, out var lease);
            AdminBedAllocation? allocation = null;
            if (lease is not null)
            {
                var paid = lease.Payments.FirstOrDefault(p => p.Status == BedLeasePaymentStatus.Paid);
                allocation = new AdminBedAllocation(
                    lease.Id,
                    lease.UserId,
                    lease.User?.DisplayName,
                    lease.StartDate,
                    lease.ExpiresOn,
                    lease.Status.ToString(),
                    lease.PriceAtAllocationCents,
                    IsPaid: paid is not null || lease.PriceAtAllocationCents == 0,
                    PaidOnUtc: paid?.PaidAtUtc);
            }

            return new AdminBed(
                bed.Id,
                bed.Code,
                bed.Section,
                bed.Number,
                bed.Label,
                bed.IsActive,
                bed.Notes,
                IsOccupied: lease is not null,
                allocation);
        }).ToList();

        var total = beds.Count(b => b.IsActive);
        var leased = bedDtos.Count(b => b.IsActive && b.IsOccupied);
        var remaining = total - leased;
        var capacity = new CapacitySummary(total, leased, remaining, remaining <= 0);

        return new LeasedBedsOverview(capacity, bedDtos);
    }

    /// <summary>The next unused bed number in a section (max existing + 1, including out-of-service beds).</summary>
    public async Task<int> NextBedNumberAsync(string section, CancellationToken ct)
    {
        var max = await db.Beds.Where(b => b.Section == section).MaxAsync(b => (int?)b.Number, ct) ?? 0;
        return max + 1;
    }

    public static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Active lease past its expiry reads as Expired for display, even though it still occupies the bed.</summary>
    public static string EffectiveStatus(BedLease lease, DateOnly today) =>
        lease.Status == BedLeaseStatus.Active && today > lease.ExpiresOn
            ? nameof(BedLeaseStatus.Expired)
            : lease.Status.ToString();

    public async Task<AdminBedRequests> GetRequestsAsync(CancellationToken ct)
    {
        var open = await db.BedRequests
            .Where(r => r.Status == BedRequestStatus.Pending || r.Status == BedRequestStatus.Waitlisted)
            .Include(r => r.User)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        var pending = open
            .Where(r => r.Status == BedRequestStatus.Pending)
            .Select(r => new AdminBedRequest(r.Id, r.UserId, r.User?.DisplayName, r.User?.Email, r.CreatedAtUtc, null))
            .ToList();

        var waitlist = open
            .Where(r => r.Status == BedRequestStatus.Waitlisted)
            .Select((r, i) => new AdminBedRequest(r.Id, r.UserId, r.User?.DisplayName, r.User?.Email, r.CreatedAtUtc, i + 1))
            .ToList();

        return new AdminBedRequests(pending, waitlist);
    }

    /// <summary>The member's in-flight request (Pending or Waitlisted), if any.</summary>
    public Task<BedRequest?> GetActiveRequestAsync(string userId, CancellationToken ct) =>
        db.BedRequests
            .Where(r => r.UserId == userId &&
                (r.Status == BedRequestStatus.Pending || r.Status == BedRequestStatus.Waitlisted))
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    public async Task<MyLeasedBedStatus> GetMyStatusAsync(ApplicationUser user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = FinancialYear.Today(now);

        var membershipActive = user.MembershipPaidThroughUtc > now;
        var canRenewMembership = !membershipActive
            || (user.MembershipPaidThroughUtc is { } expiry && FinancialYear.IsRenewalOpen(today, FinancialYear.Today(expiry)));
        var membership = new MembershipInfo(membershipActive, user.MembershipPaidThroughUtc, canRenewMembership);

        var capacity = await GetCapacityAsync(ct);

        var myLeases = await db.BedLeases
            .Where(l => l.UserId == user.Id && l.Status != BedLeaseStatus.Released)
            .Include(l => l.Bed)
            .Include(l => l.Payments)
            .ToListAsync(ct);

        var leaseDtos = myLeases
            .OrderBy(l => l.ExpiresOn)
            .Select(l =>
            {
                var paid = l.Payments.FirstOrDefault(p => p.Status == BedLeasePaymentStatus.Paid);
                var hasNewerForBed = myLeases.Any(o => o.BedId == l.BedId && o.ExpiresOn > l.ExpiresOn);
                return new MyLease(
                    l.Id,
                    l.Bed!.Code,
                    l.Bed.Label,
                    EffectiveStatus(l, today),
                    l.PriceAtAllocationCents,
                    IsPaid: paid is not null || l.PriceAtAllocationCents == 0,
                    PaidOnUtc: paid?.PaidAtUtc,
                    l.ExpiresOn,
                    PaymentDue: l.Status == BedLeaseStatus.AwaitingPayment,
                    CanRenew: FinancialYear.IsRenewalOpen(today, l.ExpiresOn) && !hasNewerForBed);
            })
            .ToList();

        var request = await GetActiveRequestAsync(user.Id, ct);
        MyRequestInfo? requestInfo = null;
        if (request is not null)
        {
            int? position = request.Status == BedRequestStatus.Waitlisted
                ? await db.BedRequests.CountAsync(
                    r => r.Status == BedRequestStatus.Waitlisted && r.CreatedAtUtc <= request.CreatedAtUtc, ct)
                : null;
            requestInfo = new MyRequestInfo(request.Status.ToString(), position);
        }

        return new MyLeasedBedStatus(membership, capacity, leaseDtos, requestInfo);
    }
}
