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

    /// <summary>The 30 June on or after <paramref name="date"/> (the end of its financial year).</summary>
    public static DateOnly GetFinancialYearEnd(DateOnly date)
    {
        var june30 = new DateOnly(date.Year, 6, 30);
        return date <= june30 ? june30 : new DateOnly(date.Year + 1, 6, 30);
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
