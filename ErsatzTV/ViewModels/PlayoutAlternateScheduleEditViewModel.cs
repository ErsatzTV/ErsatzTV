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
}
