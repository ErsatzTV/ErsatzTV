using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public class AlternateScheduleSelector
{
    public static List<DayOfWeek> AllDaysOfWeek() =>
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    public static List<int> AllDaysOfMonth() => Enumerable.Range(1, 31).ToList();

    public static List<int> AllMonthsOfYear() => Enumerable.Range(1, 12).ToList();

    public static Option<T> GetScheduleForDate<T>(IEnumerable<T> items, DateTimeOffset date)
        where T : IAlternateScheduleItem
    {
        foreach (T item in items.OrderBy(x => x.Index))
        {
            if (item.LimitToDateRange)
            {
                bool reverse = item.StartMonth * 100 + item.StartDay >
                               item.EndMonth * 100 + item.EndDay;

                int year = date.LocalDateTime.Year;
                DateTime start;
                DateTime end;

                try
                {
                    start = new DateTime(year, item.StartMonth, item.StartDay, 0, 0, 0, DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // this should only happen with days that are greater than the actual days in the month,
                    // so roll over to the 1st of the next month
                    start = new DateTime(year, item.StartMonth + 1, 1, 0, 0, 0, DateTimeKind.Local);
                }

                try
                {
                    end = new DateTime(year, item.EndMonth, item.EndDay, 0, 0, 0, DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // this should only happen with days that are greater than the actual days in the month,
                    // so reduce to the max days in the month
                    end = new DateTime(
                        year,
                        item.EndMonth,
                        DateTime.DaysInMonth(year, item.EndMonth),
                        0,
                        0,
                        0,
                        DateTimeKind.Local);
                }

                if (reverse)
                {
                    (start, end) = (end, start);
                    if (date.Date > start.Date && date.Date < end.Date)
                    {
                        continue;
                    }
                }
                else if (date.Date < start.Date || date.Date > end.Date)
                {
                    continue;
                }
            }

            bool daysOfWeek = item.DaysOfWeek.Contains(date.DayOfWeek);
            if (!daysOfWeek)
            {
                continue;
            }

            bool daysOfMonth = item.DaysOfMonth.Contains(date.Day);
            if (!daysOfMonth)
            {
                continue;
            }

            bool monthOfYear = item.MonthsOfYear.Contains(date.Month);
            if (monthOfYear)
            {
                return item;
            }
        }

        return Option<T>.None;
    }
}
