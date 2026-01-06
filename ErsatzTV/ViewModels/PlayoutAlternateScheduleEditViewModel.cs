using ErsatzTV.Application.ProgramSchedules;

namespace ErsatzTV.ViewModels;

public class PlayoutAlternateScheduleEditViewModel
{
    public int Id { get; set; }
    public int Index { get; set; }
    public ProgramScheduleViewModel ProgramSchedule { get; set; }
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
}
