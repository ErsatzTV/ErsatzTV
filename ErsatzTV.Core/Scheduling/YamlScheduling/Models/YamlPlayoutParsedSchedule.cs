using System.Globalization;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutParsedSchedule : IAlternateScheduleItem
{
    public YamlPlayoutParsedSchedule(YamlPlayoutSchedule schedule, int index)
    {
        Schedule = schedule;
        Index = index;

        // TODO: these should be sourced from the schedule
        DaysOfWeek = AlternateScheduleSelector.AllDaysOfWeek();
        DaysOfMonth = AlternateScheduleSelector.AllDaysOfMonth();
        MonthsOfYear = AlternateScheduleSelector.AllMonthsOfYear();

        Option<(int month, int day, int? year)> maybeStart = ParseDate(schedule.StartDate);
        foreach ((int month, int day, int? year) result in maybeStart)
        {
            (StartMonth, StartDay, StartYear) = result;
        }

        Option<(int month, int day, int? year)> maybeEnd = ParseDate(schedule.EndDate);
        foreach ((int month, int day, int? year) result in maybeEnd)
        {
            (EndMonth, EndDay, EndYear) = result;
        }

        if (maybeStart.IsSome && maybeEnd.IsSome)
        {
            LimitToDateRange = true;
        }
    }

    public YamlPlayoutSchedule Schedule { get; }
    public int Index { get; }
    public ICollection<DayOfWeek> DaysOfWeek { get; }
    public ICollection<int> DaysOfMonth { get; }
    public ICollection<int> MonthsOfYear { get; }
    public bool LimitToDateRange { get; }
    public int StartMonth { get; }
    public int StartDay { get; }
    public int? StartYear { get; }
    public int EndMonth { get; }
    public int EndDay { get; }
    public int? EndYear { get; }

    /// <summary>
    /// Parses a date string in MM-DD or YYYY-MM-DD format.
    /// Returns (month, day, year) where year is null for MM-DD format.
    /// </summary>
    private static Option<(int month, int day, int? year)> ParseDate(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
        {
            return None;
        }

        string[] parts = dateStr.Split('-');

        if (parts.Length == 2)
        {
            // MM-DD format
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture), null);
        }

        if (parts.Length == 3)
        {
            // YYYY-MM-DD format
            return (int.Parse(parts[1], CultureInfo.InvariantCulture), int.Parse(parts[2], CultureInfo.InvariantCulture), int.Parse(parts[0], CultureInfo.InvariantCulture));
        }

        throw new FormatException($"Invalid date format: {dateStr}. Expected MM-DD or YYYY-MM-DD.");
    }
}
