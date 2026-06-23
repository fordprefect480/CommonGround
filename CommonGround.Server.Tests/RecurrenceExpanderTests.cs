using CommonGround.Server.Events;

namespace CommonGround.Server.Tests;

public class RecurrenceExpanderTests
{
    // Adelaide is UTC+9:30 (standard) / UTC+10:30 (daylight, Oct–Apr).
    // Helper: build the UTC instant for a given Adelaide-local wall time.
    private static DateTime AdelaideToUtc(int year, int month, int day, int hour, int minute)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private static DateOnly AdelaideDateOf(DateTime utc)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz));
    }

    [Fact]
    public void ParseFrequency_is_case_insensitive_and_defaults_to_none()
    {
        Assert.Equal(RepeatFrequency.Weekly, RecurrenceExpander.ParseFrequency("Weekly"));
        Assert.Equal(RepeatFrequency.Monthly, RecurrenceExpander.ParseFrequency("monthly"));
        Assert.Equal(RepeatFrequency.None, RecurrenceExpander.ParseFrequency("nonsense"));
        Assert.Equal(RepeatFrequency.None, RecurrenceExpander.ParseFrequency(null));
    }

    [Fact]
    public void Weekly_steps_seven_days_and_includes_the_inclusive_end()
    {
        // Sat 2 May 2026 09:00, weekly until Sat 30 May 2026 → 2,9,16,23,30 = 5 occurrences.
        var start = AdelaideToUtc(2026, 5, 2, 9, 0);
        var result = RecurrenceExpander.Expand(start, RepeatFrequency.Weekly, new DateOnly(2026, 5, 30));
        var dates = result.Select(AdelaideDateOf).ToList();
        Assert.Equal(
            new[] { new DateOnly(2026,5,2), new DateOnly(2026,5,9), new DateOnly(2026,5,16), new DateOnly(2026,5,23), new DateOnly(2026,5,30) },
            dates);
    }

    [Fact]
    public void Weekly_excludes_an_occurrence_one_day_past_the_until_date()
    {
        var start = AdelaideToUtc(2026, 5, 2, 9, 0);
        var result = RecurrenceExpander.Expand(start, RepeatFrequency.Weekly, new DateOnly(2026, 5, 8));
        Assert.Single(result); // only 2 May; 9 May is past 8 May.
    }

    [Fact]
    public void Fortnightly_steps_fourteen_days()
    {
        // Sat 2 May 2026, fortnightly until 31 May → 2, 16, 30 = 3 occurrences.
        var start = AdelaideToUtc(2026, 5, 2, 9, 0);
        var dates = RecurrenceExpander.Expand(start, RepeatFrequency.Fortnightly, new DateOnly(2026, 5, 31))
            .Select(AdelaideDateOf).ToList();
        Assert.Equal(new[] { new DateOnly(2026,5,2), new DateOnly(2026,5,16), new DateOnly(2026,5,30) }, dates);
    }

    [Fact]
    public void Monthly_repeats_on_the_same_nth_weekday()
    {
        // 16 May 2026 is the 3rd Saturday. Monthly until 31 Aug → 3rd Saturdays: May16, Jun20, Jul18, Aug15.
        var start = AdelaideToUtc(2026, 5, 16, 9, 0);
        var dates = RecurrenceExpander.Expand(start, RepeatFrequency.Monthly, new DateOnly(2026, 8, 31))
            .Select(AdelaideDateOf).ToList();
        Assert.Equal(
            new[] { new DateOnly(2026,5,16), new DateOnly(2026,6,20), new DateOnly(2026,7,18), new DateOnly(2026,8,15) },
            dates);
    }

    [Fact]
    public void Monthly_skips_months_without_the_nth_weekday()
    {
        // 30 May 2026 is the 5th Saturday. June & July 2026 have no 5th Saturday; Aug does (29th).
        var start = AdelaideToUtc(2026, 5, 30, 9, 0);
        var dates = RecurrenceExpander.Expand(start, RepeatFrequency.Monthly, new DateOnly(2026, 8, 31))
            .Select(AdelaideDateOf).ToList();
        Assert.Equal(new[] { new DateOnly(2026,5,30), new DateOnly(2026,8,29) }, dates);
    }

    [Fact]
    public void Weekly_preserves_local_time_of_day_across_a_dst_transition()
    {
        // Adelaide DST starts Sun 5 Oct 2025. Start Sat 4 Oct 09:00 local, weekly until 18 Oct.
        var start = AdelaideToUtc(2025, 10, 4, 9, 0);
        var result = RecurrenceExpander.Expand(start, RepeatFrequency.Weekly, new DateOnly(2025, 10, 18));
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");
        foreach (var utc in result)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
            Assert.Equal(new TimeSpan(9, 0, 0), local.TimeOfDay); // still 09:00 after DST starts
        }
        Assert.Equal(3, result.Count); // 4, 11, 18 Oct
    }

    [Fact]
    public void Generation_is_capped_at_the_maximum()
    {
        var start = AdelaideToUtc(2026, 5, 2, 9, 0);
        var result = RecurrenceExpander.Expand(start, RepeatFrequency.Weekly, new DateOnly(2036, 5, 2));
        Assert.Equal(RecurrenceExpander.MaxOccurrences, result.Count);
    }

    [Fact]
    public void First_occurrence_equals_the_supplied_start()
    {
        var start = AdelaideToUtc(2026, 5, 2, 9, 0);
        var result = RecurrenceExpander.Expand(start, RepeatFrequency.Weekly, new DateOnly(2026, 5, 30));
        Assert.Equal(start, result[0]);
    }
}
