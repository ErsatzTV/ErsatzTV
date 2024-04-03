using ErsatzTV.Application.Scheduling;
using MudBlazor;

namespace ErsatzTV.ViewModels;

public class PlayoutTemplateEditViewModel
{
    public int Id { get; set; }
    public int Index { get; set; }
    public TemplateViewModel Template { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; }
    public List<int> DaysOfMonth { get; set; }
    public List<int> MonthsOfYear { get; set; }
    public bool LimitToDateRange { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateRange ActiveDateRange
    {
        get
        {
            if (StartDate is null || EndDate is null || StartDate.Value.Year < 2000 || EndDate.Value.Year < 2000)
            {
                return new DateRange(
                    DateTime.Today,
                    new DateTime(3000, 1, 1, 0, 0, 0, DateTimeKind.Local));
            }

            return new DateRange(StartDate.Value.LocalDateTime, EndDate.Value.LocalDateTime);
        }

        set
        {
            StartDate = null;
            if (value?.Start is not null)
            {
                DateTime start = value.Start.Value;
                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(
                    new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Local));
                StartDate = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, offset);
            }

            EndDate = null;
            if (value?.End is not null)
            {
                DateTime end = value.End.Value;
                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(
                    new DateTime(end.Year, end.Month, end.Day, 0, 0, 0, DateTimeKind.Local));
                EndDate = new DateTimeOffset(end.Year, end.Month, end.Day, 0, 0, 0, offset);
            }
        }
    }

    public bool AppliesToDate(DateTime date) =>
        (LimitToDateRange is false || StartDate is null || date.Date >= StartDate.Value.Date) &&
        (LimitToDateRange is false || EndDate is null || date.Date <= EndDate.Value.Date) &&
        DaysOfWeek.Contains(date.DayOfWeek) &&
        DaysOfMonth.Contains(date.Day) &&
        MonthsOfYear.Contains(date.Month);
}
