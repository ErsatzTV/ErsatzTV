namespace ErsatzTV.Application.Scheduling;

public record PlayoutTemplateViewModel(
    int Id,
    TemplateViewModel Template,
    DecoTemplateViewModel DecoTemplate,
    int Index,
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
