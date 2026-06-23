using CommonGround.Server.LeasedBeds;

namespace CommonGround.Server.Tests;

public class FinancialYearTests
{
    [Fact]
    public void End_for_a_date_before_June30_is_June30_that_year()
    {
        var end = FinancialYear.GetFinancialYearEnd(new DateOnly(2026, 5, 10));
        Assert.Equal(new DateOnly(2026, 6, 30), end);
    }

    [Fact]
    public void End_for_a_date_after_June30_is_June30_next_year()
    {
        var end = FinancialYear.GetFinancialYearEnd(new DateOnly(2026, 8, 1));
        Assert.Equal(new DateOnly(2027, 6, 30), end);
    }

    [Fact]
    public void End_for_June30_itself_is_that_same_June30()
    {
        var end = FinancialYear.GetFinancialYearEnd(new DateOnly(2026, 6, 30));
        Assert.Equal(new DateOnly(2026, 6, 30), end);
    }

    [Fact]
    public void End_for_July1_is_June30_next_year()
    {
        // This is the renewal case: StartDate = previous ExpiresOn + 1 day.
        var end = FinancialYear.GetFinancialYearEnd(new DateOnly(2026, 7, 1));
        Assert.Equal(new DateOnly(2027, 6, 30), end);
    }

    [Fact]
    public void End_for_early_in_the_calendar_year_is_June30_that_year()
    {
        var end = FinancialYear.GetFinancialYearEnd(new DateOnly(2026, 1, 1));
        Assert.Equal(new DateOnly(2026, 6, 30), end);
    }

    [Fact]
    public void Renewing_chains_to_the_next_financial_year()
    {
        var currentEnd = new DateOnly(2026, 6, 30);
        var renewalStart = currentEnd.AddDays(1);
        var renewalEnd = FinancialYear.GetFinancialYearEnd(renewalStart);
        Assert.Equal(new DateOnly(2026, 7, 1), renewalStart);
        Assert.Equal(new DateOnly(2027, 6, 30), renewalEnd);
    }

    [Fact]
    public void New_lease_within_8_weeks_of_June30_carries_to_the_following_year()
    {
        // Assigning on 23 June 2026 is only days from EOFY, so the term runs to 30 June 2027.
        Assert.Equal(new DateOnly(2027, 6, 30), FinancialYear.GetNewLeaseTermEnd(new DateOnly(2026, 6, 23)));
    }

    [Fact]
    public void New_lease_exactly_56_days_before_June30_still_carries_over()
    {
        // 5 May 2026 is 56 days before 30 June 2026 (the carry-over boundary, inclusive).
        Assert.Equal(new DateOnly(2027, 6, 30), FinancialYear.GetNewLeaseTermEnd(new DateOnly(2026, 5, 5)));
    }

    [Fact]
    public void New_lease_more_than_8_weeks_before_June30_expires_that_same_year()
    {
        // 4 May 2026 is 57 days before 30 June 2026 - just outside the window, so a partial year.
        Assert.Equal(new DateOnly(2026, 6, 30), FinancialYear.GetNewLeaseTermEnd(new DateOnly(2026, 5, 4)));
    }

    [Fact]
    public void Today_uses_Adelaide_local_date_late_on_June30()
    {
        // 30 June 2026 23:00 Adelaide (UTC+9:30) == 30 June 2026 13:30 UTC.
        var utc = new DateTime(2026, 6, 30, 13, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 6, 30), FinancialYear.Today(utc));
    }

    [Fact]
    public void Today_rolls_to_July1_when_Adelaide_has_passed_midnight_but_UTC_has_not()
    {
        // 1 July 2026 00:30 Adelaide == 30 June 2026 15:00 UTC.
        // A naive UTC .Date would still read 30 June - this guards that boundary.
        var utc = new DateTime(2026, 6, 30, 15, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 7, 1), FinancialYear.Today(utc));
    }

    [Fact]
    public void RenewalOpens_one_month_before_expiry()
    {
        var expiresOn = new DateOnly(2026, 6, 30);
        Assert.False(FinancialYear.IsRenewalOpen(new DateOnly(2026, 5, 29), expiresOn));
        Assert.True(FinancialYear.IsRenewalOpen(new DateOnly(2026, 5, 30), expiresOn));
        Assert.True(FinancialYear.IsRenewalOpen(new DateOnly(2026, 6, 30), expiresOn));
        Assert.True(FinancialYear.IsRenewalOpen(new DateOnly(2026, 7, 15), expiresOn)); // after expiry
    }
}
