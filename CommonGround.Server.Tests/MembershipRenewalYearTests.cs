using CommonGround.Server.Members;

namespace CommonGround.Server.Tests;

public class MembershipRenewalYearTests
{
    // 1 July in Adelaide (UTC+9:30 in winter) is the prior 30 June 14:30 UTC.
    private static DateTime AdelaideJuly1Utc(int year) => new(year, 6, 30, 14, 30, 0, DateTimeKind.Utc);

    // A "now" inside the 8-week carry-over window before 1 July 2026, so the
    // renewal year the garden is collecting for is FY2026/27 (ends 1 July 2027).
    private static readonly DateTime DuringCarryOver = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    // A "now" well inside FY2025/26 (no carry-over), so the renewal year is the
    // current one, FY2025/26 (ends 1 July 2026).
    private static readonly DateTime MidYear = new(2026, 2, 28, 13, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void RenewalYearStart_is_one_year_before_the_paid_through_target()
    {
        Assert.Equal(AdelaideJuly1Utc(2026), MembershipPeriod.RenewalYearStart(DuringCarryOver));
        Assert.Equal(AdelaideJuly1Utc(2025), MembershipPeriod.RenewalYearStart(MidYear));
    }

    [Fact]
    public void Never_paid_is_not_paid()
    {
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(null, DuringCarryOver));
    }

    [Fact]
    public void Covered_through_the_upcoming_year_is_paid()
    {
        // Paid through 1 July 2027 (end of FY2026/27) during the carry-over window.
        Assert.True(MembershipPeriod.IsPaidForRenewalYear(AdelaideJuly1Utc(2027), DuringCarryOver));
    }

    [Fact]
    public void Covered_only_through_the_current_year_is_not_paid_during_carryover()
    {
        // Paid through 1 July 2026 exactly (the renewal-year START): still owes
        // for FY2026/27, so this is the cohort an admin would remind.
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(AdelaideJuly1Utc(2026), DuringCarryOver));
    }

    [Fact]
    public void Covered_to_the_last_day_before_the_boundary_is_still_paid()
    {
        // The bug this guards: a paid-through that lands just before the exact
        // 1 July 2027 boundary (e.g. stored as 30 June 2027 midnight UTC) still
        // covers FY2026/27 and must read as paid, not "not yet paid".
        var justBeforeBoundary = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(MembershipPeriod.IsPaidForRenewalYear(justBeforeBoundary, DuringCarryOver));
    }

    [Fact]
    public void Lapsed_membership_is_not_paid()
    {
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(AdelaideJuly1Utc(2025), DuringCarryOver));
    }

    [Fact]
    public void Mid_year_paid_for_the_current_year_is_paid()
    {
        // No carry-over: renewal year is FY2025/26 (start 1 July 2025), so a
        // member covered through 1 July 2026 is paid, and one covered only
        // through 1 July 2025 is not.
        Assert.True(MembershipPeriod.IsPaidForRenewalYear(AdelaideJuly1Utc(2026), MidYear));
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(AdelaideJuly1Utc(2025), MidYear));
    }
}
