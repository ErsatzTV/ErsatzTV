using ErsatzTV.Annotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.ViewModels;

public class PlayoutTemplateEditViewModel
{
    private int _startMonth;
    private int _startDay;
    private int _endMonth;
    private int _endDay;
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
        get => _startMonth == 0 ? 1 : _startMonth;
        set => _startMonth = value;
    }

    public int StartDay
    {
        get => _startDay == 0 ? 1 : _startDay;
        set => _startDay = value;
    }

    public int EndMonth
    {
        get => _endMonth == 0 ? 12 : _endMonth;
        set => _endMonth = value;
    }

    public int EndDay
    {
        get => _endDay == 0 ? 31 : _endDay;
        set => _endDay = value;
    }

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
            EndMonth = EndMonth,
            EndDay = EndDay,
        };

        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(
            new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Local));

        Option<PlayoutTemplate> result =
            PlayoutTemplateSelector.GetPlayoutTemplateFor(
                new[] { template },
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
