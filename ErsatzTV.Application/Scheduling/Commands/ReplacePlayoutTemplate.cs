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
    int? StartYear,
    int EndMonth,
    int EndDay,
    int? EndYear);
