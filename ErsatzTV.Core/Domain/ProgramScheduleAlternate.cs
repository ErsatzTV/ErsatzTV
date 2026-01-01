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

    // placeholder data; migration will be added later
    public bool LimitToDateRange => false;
    public int StartMonth => 0;
    public int StartDay => 0;
    public int EndMonth => 0;
    public int EndDay => 0;
}
