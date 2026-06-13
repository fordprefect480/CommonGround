using CommonGround.Server.Members;

namespace CommonGround.Server.Tests;

public class MembershipPeriodTests
{
    // 1 July in Adelaide (UTC+9:30 in winter) is 2025-06-30T14:30:00Z.
    private static DateTime AdelaideJuly1Utc(int year) => new(year, 6, 30, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Joining_well_before_July_pays_through_the_upcoming_July1()
    {
        // 1 March 2026 (Adelaide) -> 2026-02-28T13:30:00Z
        var now = new DateTime(2026, 2, 28, 13, 30, 0, DateTimeKind.Utc);
        var result = MembershipPeriod.ComputePaidThrough(now);
        Assert.Equal(AdelaideJuly1Utc(2026), result);
    }

    [Fact]
    public void Joining_within_8_weeks_before_July_carries_to_the_following_year()
    {
        // 9 June 2026 Adelaide (~22 days before 1 July) -> carries to 2027.
        var now = new DateTime(2026, 6, 9, 13, 30, 0, DateTimeKind.Utc);
        var result = MembershipPeriod.ComputePaidThrough(now);
        Assert.Equal(AdelaideJuly1Utc(2027), result);
    }

    [Fact]
    public void Joining_just_after_July1_pays_through_next_year_July1()
    {
        // 2 July 2026 Adelaide -> 2026-07-01T14:30:00Z (early morning local of 2 July)
        var now = new DateTime(2026, 7, 2, 2, 0, 0, DateTimeKind.Utc);
        var result = MembershipPeriod.ComputePaidThrough(now);
        Assert.Equal(AdelaideJuly1Utc(2027), result);
    }

    [Fact]
    public void Joining_exactly_8_weeks_before_July1_carries_over()
    {
        // 56 days before 1 July 2026 = 6 May 2026 Adelaide.
        var now = new DateTime(2026, 5, 5, 14, 30, 0, DateTimeKind.Utc); // 6 May 2026 Adelaide
        var result = MembershipPeriod.ComputePaidThrough(now);
        Assert.Equal(AdelaideJuly1Utc(2027), result);
    }

    [Fact]
    public void Joining_just_over_8_weeks_before_July1_does_not_carry()
    {
        // 4 May 2026 Adelaide (58 days before 1 July) -> stays 2026.
        var now = new DateTime(2026, 5, 3, 14, 30, 0, DateTimeKind.Utc); // 4 May 2026 Adelaide
        var result = MembershipPeriod.ComputePaidThrough(now);
        Assert.Equal(AdelaideJuly1Utc(2026), result);
    }
}
