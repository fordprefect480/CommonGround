using CommonGround.Server.Data;
using CommonGround.Server.LeasedBeds;

namespace CommonGround.Server.Tests;

public class EffectiveLeaseStatusTests
{
    private static BedLease Lease(BedLeaseStatus status, DateOnly expiresOn) =>
        new() { Status = status, ExpiresOn = expiresOn };

    [Fact]
    public void Active_before_expiry_stays_active()
    {
        var lease = Lease(BedLeaseStatus.Active, new DateOnly(2026, 6, 30));
        Assert.Equal("Active", LeasedBedService.EffectiveStatus(lease, new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void Active_on_expiry_day_stays_active()
    {
        var lease = Lease(BedLeaseStatus.Active, new DateOnly(2026, 6, 30));
        Assert.Equal("Active", LeasedBedService.EffectiveStatus(lease, new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void Active_past_expiry_reads_as_expired()
    {
        var lease = Lease(BedLeaseStatus.Active, new DateOnly(2026, 6, 30));
        Assert.Equal("Expired", LeasedBedService.EffectiveStatus(lease, new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void AwaitingPayment_is_never_rewritten_to_expired()
    {
        var lease = Lease(BedLeaseStatus.AwaitingPayment, new DateOnly(2026, 6, 30));
        Assert.Equal("AwaitingPayment", LeasedBedService.EffectiveStatus(lease, new DateOnly(2026, 7, 1)));
    }
}
