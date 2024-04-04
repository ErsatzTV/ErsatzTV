namespace ErsatzTV.Application.Scheduling;

public record ReplacePlayoutTemplate(
    int Id,
    int Index,
    int TemplateId,
    int? DecoTemplateId,
    List<DayOfWeek> DaysOfWeek,
    List<int> DaysOfMonth,
    List<int> MonthsOfYear,
    bool LimitToDateRange,
    int StartMonth,
    int StartDay,
    int EndMonth,
    int EndDay);
