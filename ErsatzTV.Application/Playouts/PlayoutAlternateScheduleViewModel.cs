namespace ErsatzTV.Application.Playouts;

public record PlayoutAlternateScheduleViewModel(
    int Id,
    int Index,
    int ProgramScheduleId,
    ICollection<DayOfWeek> DaysOfWeek,
    ICollection<int> DaysOfMonth,
    ICollection<int> MonthsOfYear);
