namespace CommonGround.Server.Events;

public enum RepeatFrequency
{
    None,
    Weekly,
    Fortnightly,
    Monthly,
}

/// <summary>
/// Expands a recurring community event into individual occurrence start times.
/// Stepping is done in the garden's local (Adelaide) time so an event keeps its
/// local time-of-day across daylight-saving transitions; results are returned in UTC.
/// </summary>
public static class RecurrenceExpander
{
    public const int MaxOccurrences = 104;

    private static readonly TimeZoneInfo GardenTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");

    public static RepeatFrequency ParseFrequency(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "weekly" => RepeatFrequency.Weekly,
        "fortnightly" => RepeatFrequency.Fortnightly,
        "monthly" => RepeatFrequency.Monthly,
        _ => RepeatFrequency.None,
    };

    /// <summary>The Adelaide-local calendar date of a UTC instant.</summary>
    public static DateOnly GardenDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    /// <summary>
    /// Occurrence start times (UTC) from <paramref name="startUtc"/> up to and including
    /// <paramref name="until"/> (local date), capped at <see cref="MaxOccurrences"/>.
    /// The first occurrence is always the supplied start. Do not call with <see cref="RepeatFrequency.None"/>.
    /// </summary>
    public static IReadOnlyList<DateTime> Expand(DateTime startUtc, RepeatFrequency frequency, DateOnly until)
    {
        var local = ToLocal(startUtc);
        var occurrences = new List<DateTime> { startUtc };

        if (frequency is RepeatFrequency.Weekly or RepeatFrequency.Fortnightly)
        {
            var step = frequency == RepeatFrequency.Weekly ? 7 : 14;
            var next = local.AddDays(step);
            while (DateOnly.FromDateTime(next) <= until && occurrences.Count < MaxOccurrences)
            {
                occurrences.Add(ToUtc(next));
                next = next.AddDays(step);
            }
        }
        else if (frequency == RepeatFrequency.Monthly)
        {
            var weekday = local.DayOfWeek;
            var ordinal = (local.Day - 1) / 7 + 1; // 1..5
            var timeOfDay = local.TimeOfDay;
            var firstOfStartMonth = new DateTime(local.Year, local.Month, 1);

            for (var monthOffset = 1; occurrences.Count < MaxOccurrences; monthOffset++)
            {
                var month = firstOfStartMonth.AddMonths(monthOffset);
                if (new DateOnly(month.Year, month.Month, 1) > until) break;

                var day = NthWeekdayOfMonth(month.Year, month.Month, weekday, ordinal);
                if (day is null) continue; // e.g. no 5th Saturday this month

                var occurrenceDate = new DateOnly(month.Year, month.Month, day.Value);
                if (occurrenceDate > until) break;

                occurrences.Add(ToUtc(occurrenceDate.ToDateTime(TimeOnly.FromTimeSpan(timeOfDay))));
            }
        }

        return occurrences;
    }

    /// <summary>The day-of-month of the <paramref name="ordinal"/>-th <paramref name="weekday"/>, or null if absent.</summary>
    private static int? NthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int ordinal)
    {
        var firstOfMonth = new DateTime(year, month, 1);
        var offset = ((int)weekday - (int)firstOfMonth.DayOfWeek + 7) % 7;
        var day = 1 + offset + (ordinal - 1) * 7;
        return day <= DateTime.DaysInMonth(year, month) ? day : null;
    }

    private static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), GardenTimeZone);

    private static DateTime ToUtc(DateTime local)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (GardenTimeZone.IsInvalidTime(local))
            local = local.AddHours(1); // shift past the spring-forward gap onto a valid wall time
        return TimeZoneInfo.ConvertTimeToUtc(local, GardenTimeZone);
    }
}
