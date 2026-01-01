namespace ErsatzTV.Application.Playouts;

public record ReplacePlayoutAlternateSchedule(
    int Id,
    int Index,
    int ProgramScheduleId,
    List<DayOfWeek> DaysOfWeek,
    List<int> DaysOfMonth,
    List<int> MonthsOfYear,
    bool LimitToDateRange,
    int StartMonth,
    int StartDay,
    int? StartYear,
    int EndMonth,
    int EndDay,
    int? EndYear);
