namespace CommonGround.Server.Members;

/// <summary>
/// Computes a membership's paid-through date from the renewal policy:
/// membership runs to the next 1 July (Adelaide local), and joining within
/// 8 weeks of that 1 July carries the membership to the following year.
/// </summary>
public static class MembershipPeriod
{
    private static readonly TimeZoneInfo GardenTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");

    private const int CarryOverDays = 56; // 8 weeks

    public static DateTime ComputePaidThrough(DateTime nowUtc)
    {
        var utc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(utc, GardenTimeZone).Date;

        var july1ThisYear = new DateTime(localDate.Year, 7, 1);
        var nextJuly1 = localDate < july1ThisYear
            ? july1ThisYear
            : july1ThisYear.AddYears(1);

        if ((nextJuly1 - localDate).TotalDays <= CarryOverDays)
        {
            nextJuly1 = nextJuly1.AddYears(1);
        }

        var endLocal = DateTime.SpecifyKind(nextJuly1, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(endLocal, GardenTimeZone);
    }

    /// <summary>
    /// The start (1 July) of the financial year that the next renewal covers,
    /// evaluated at <paramref name="nowUtc"/>. This is exactly one year before
    /// <see cref="ComputePaidThrough"/>, since membership years are a year long.
    /// </summary>
    public static DateTime RenewalYearStart(DateTime nowUtc) =>
        ComputePaidThrough(nowUtc).AddYears(-1);

    /// <summary>
    /// Whether a membership with the given <paramref name="paidThroughUtc"/> is
    /// paid up for the financial year the next renewal covers, evaluated at
    /// <paramref name="nowUtc"/>. Coverage is tested against the START of that
    /// year (see <see cref="RenewalYearStart"/>) so a member covered through to
    /// the year's end counts as paid even when their paid-through instant sits a
    /// little before the exact 1 July boundary; comparing against the year end
    /// instead marked fully-covered members as unpaid.
    /// </summary>
    public static bool IsPaidForRenewalYear(DateTime? paidThroughUtc, DateTime nowUtc) =>
        paidThroughUtc is { } paidThrough && paidThrough > RenewalYearStart(nowUtc);
}
