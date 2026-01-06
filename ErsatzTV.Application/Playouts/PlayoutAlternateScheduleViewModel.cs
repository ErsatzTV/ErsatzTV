namespace ErsatzTV.Application.Playouts;

public record PlayoutAlternateScheduleViewModel(
    int Id,
    int Index,
    int ProgramScheduleId,
    ICollection<DayOfWeek> DaysOfWeek,
    ICollection<int> DaysOfMonth,
    ICollection<int> MonthsOfYear,
    bool LimitToDateRange,
    int StartMonth,
    int StartDay,
    int? StartYear,
    int EndMonth,
    int EndDay,
    int? EndYear);
