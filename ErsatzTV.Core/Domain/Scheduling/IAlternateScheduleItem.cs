namespace ErsatzTV.Core.Domain.Scheduling;

public interface IAlternateScheduleItem
{
    int Index { get; }
    ICollection<DayOfWeek> DaysOfWeek { get; }
    ICollection<int> DaysOfMonth { get; }
    ICollection<int> MonthsOfYear { get; }
    bool LimitToDateRange { get; }
    int StartMonth { get; }
    int StartDay { get; }
    int EndMonth { get; }
    int EndDay { get; }
}
