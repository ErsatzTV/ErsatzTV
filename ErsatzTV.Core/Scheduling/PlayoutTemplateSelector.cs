using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public static class PlayoutTemplateSelector
{
    public static Option<PlayoutTemplate> GetPlayoutTemplateFor(
        IEnumerable<PlayoutTemplate> templates,
        DateTimeOffset date)
    {
        foreach (PlayoutTemplate template in templates.OrderBy(x => x.Index))
        {
            if (template.LimitToDateRange)
            {
                bool reverse = template.StartMonth * 100 + template.StartDay >
                               template.EndMonth * 100 + template.EndDay;

                int year = date.LocalDateTime.Year;
                DateTime start;
                DateTime end;

                try
                {
                    start = new DateTime(year, template.StartMonth, template.StartDay, 0, 0, 0, DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // this should only happen with days that are greater than the actual days in the month,
                    // so roll over to the 1st of the next month
                    start = new DateTime(year, template.StartMonth + 1, 1, 0, 0, 0, DateTimeKind.Local);
                }

                try
                {
                    end = new DateTime(year, template.EndMonth, template.EndDay, 0, 0, 0, DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // this should only happen with days that are greater than the actual days in the month,
                    // so reduce to the max days in the month
                    end = new DateTime(
                        year,
                        template.EndMonth,
                        DateTime.DaysInMonth(year, template.EndMonth),
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

            bool daysOfWeek = template.DaysOfWeek.Contains(date.DayOfWeek);
            if (!daysOfWeek)
            {
                continue;
            }

            bool daysOfMonth = template.DaysOfMonth.Contains(date.Day);
            if (!daysOfMonth)
            {
                continue;
            }

            bool monthOfYear = template.MonthsOfYear.Contains(date.Month);
            if (monthOfYear)
            {
                return template;
            }
        }

        return Option<PlayoutTemplate>.None;
    }
}
