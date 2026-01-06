using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class ProgramScheduleAlternate : IAlternateScheduleItem
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public int ProgramScheduleId { get; set; }
    public ProgramSchedule ProgramSchedule { get; set; }
    public int Index { get; set; }
    public ICollection<DayOfWeek> DaysOfWeek { get; set; }
    public ICollection<int> DaysOfMonth { get; set; }
    public ICollection<int> MonthsOfYear { get; set; }
    public bool LimitToDateRange { get; set; }
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int? StartYear { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }
    public int? EndYear { get; set; }
}
