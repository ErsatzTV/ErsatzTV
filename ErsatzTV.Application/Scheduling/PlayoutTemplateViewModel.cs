namespace ErsatzTV.Application.Scheduling;

public record PlayoutTemplateViewModel(
    int Id,
    TemplateViewModel Template,
    int Index,
    ICollection<DayOfWeek> DaysOfWeek,
    ICollection<int> DaysOfMonth,
    ICollection<int> MonthsOfYear);
