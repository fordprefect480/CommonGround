namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Australian financial-year arithmetic for the leased-bed lifecycle. The year
/// ends on 30 June, and "today" is always evaluated in Adelaide local time so a
/// term never expires a day early (or late) around the UTC boundary.
/// </summary>
public static class FinancialYear
{
    private static readonly TimeZoneInfo GardenTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");

    private const int CarryOverDays = 56; // 8 weeks, matching MembershipPeriod's late-join carry-over.

    /// <summary>The 30 June on or after <paramref name="date"/> (the end of its financial year).</summary>
    public static DateOnly GetFinancialYearEnd(DateOnly date)
    {
        var june30 = new DateOnly(date.Year, 6, 30);
        return date <= june30 ? june30 : new DateOnly(date.Year + 1, 6, 30);
    }

    /// <summary>
    /// The expiry for a new lease starting on <paramref name="date"/>. Mirrors the membership
    /// policy: starting within 8 weeks of 30 June carries the term to the following 30 June, so a
    /// late-year lease isn't charged a full year's fee for only a few days.
    /// </summary>
    public static DateOnly GetNewLeaseTermEnd(DateOnly date)
    {
        var end = GetFinancialYearEnd(date);
        return end.DayNumber - date.DayNumber <= CarryOverDays ? end.AddYears(1) : end;
    }

    /// <summary>The current date in the garden's local (Adelaide) time zone.</summary>
    public static DateOnly Today(DateTime nowUtc)
    {
        var utc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, GardenTimeZone));
    }

    /// <summary>
    /// Whether a term expiring on <paramref name="expiresOn"/> can be renewed yet:
    /// renewals open one month before expiry and remain open through and after it.
    /// </summary>
    public static bool IsRenewalOpen(DateOnly today, DateOnly expiresOn) =>
        today >= expiresOn.AddMonths(-1);
}
