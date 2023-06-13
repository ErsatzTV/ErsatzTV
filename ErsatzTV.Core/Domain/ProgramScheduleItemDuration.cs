namespace ErsatzTV.Core.Domain;

public class ProgramScheduleItemDuration : ProgramScheduleItem
{
    public TimeSpan PlayoutDuration { get; set; }
    public TailMode TailMode { get; set; }
    public int DiscardToFillAttempts { get; set; }
}
