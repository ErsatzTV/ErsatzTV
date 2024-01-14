namespace ErsatzTV.Application.Scheduling;

public record ReplacePlayoutTemplate(
    int Id,
    int Index,
    int TemplateId,
    List<DayOfWeek> DaysOfWeek,
    List<int> DaysOfMonth,
    List<int> MonthsOfYear);
