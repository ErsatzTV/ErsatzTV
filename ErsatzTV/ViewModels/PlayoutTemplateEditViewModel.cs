using ErsatzTV.Annotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.ViewModels;

public class PlayoutTemplateEditViewModel
{
    public int Id { get; set; }
    public int Index { get; set; }
    public TemplateViewModel Template { get; set; }

    [CanBeNull]
    public DecoTemplateViewModel DecoTemplate { get; set; }

    public List<DayOfWeek> DaysOfWeek { get; set; }
    public List<int> DaysOfMonth { get; set; }
    public List<int> MonthsOfYear { get; set; }
    public bool LimitToDateRange { get; set; }

    public int StartMonth
    {
        get => field == 0 ? 1 : field;
        set;
    }

    public int StartDay
    {
        get => field == 0 ? 1 : field;
        set;
    }

    public int? StartYear { get; set; }

    public int EndMonth
    {
        get => field == 0 ? 12 : field;
        set;
    }

    public int EndDay
    {
        get => field == 0 ? 31 : field;
        set;
    }

    public int? EndYear { get; set; }

    public bool AppliesToDate(DateTime date)
    {
        // share the PlayoutTemplateSelector logic

        var template = new PlayoutTemplate
        {
            DaysOfWeek = DaysOfWeek,
            DaysOfMonth = DaysOfMonth,
            MonthsOfYear = MonthsOfYear,
            LimitToDateRange = LimitToDateRange,
            StartMonth = StartMonth,
            StartDay = StartDay,
            StartYear = StartYear,
            EndMonth = EndMonth,
            EndDay = EndDay,
            EndYear = EndYear
        };

        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(
            new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local));

        Option<PlayoutTemplate> result =
            AlternateScheduleSelector.GetScheduleForDate(
                [template],
                new DateTimeOffset(
                    date.Year,
                    date.Month,
                    date.Day,
                    0,
                    0,
                    0,
                    offset));

        return result.IsSome;
    }
}
