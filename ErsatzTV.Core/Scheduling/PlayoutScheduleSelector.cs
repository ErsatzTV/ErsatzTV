using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

public static class PlayoutScheduleSelector
{
    public static ProgramSchedule GetProgramScheduleFor(
        ProgramSchedule defaultSchedule,
        IEnumerable<ProgramScheduleAlternate> alternates,
        DateTimeOffset date)
    {
        foreach (ProgramScheduleAlternate alternate in alternates.OrderBy(x => x.Index))
        {
            bool daysOfWeek = alternate.DaysOfWeek.Count is 0 or 7 ||
                              alternate.DaysOfWeek.Contains(date.DayOfWeek);

            if (!daysOfWeek)
            {
                continue;
            }

            bool daysOfMonth = alternate.DaysOfMonth.Count is 0 or 31 ||
                               alternate.DaysOfMonth.Contains(date.Day);
            if (!daysOfMonth)
            {
                continue;
            }

            bool monthOfYear = alternate.MonthsOfYear.Count is 0 or 12 ||
                               alternate.MonthsOfYear.Contains(date.Month);

            if (monthOfYear)
            {
                return alternate.ProgramSchedule;
            }
        }

        return defaultSchedule;
    }
}
