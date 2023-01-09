namespace ErsatzTV.Application.Playouts;

public record ReplacePlayoutAlternateSchedule(
    int Id,
    int Index,
    int ProgramScheduleId,
    List<DayOfWeek> DaysOfWeek,
    List<int> DaysOfMonth,
    List<int> MonthsOfYear);
