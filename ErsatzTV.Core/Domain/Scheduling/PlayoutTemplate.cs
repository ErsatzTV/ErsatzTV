namespace ErsatzTV.Core.Domain.Scheduling;

public class PlayoutTemplate : IAlternateScheduleItem
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public int TemplateId { get; set; }
    public Template Template { get; set; }
    public int? DecoTemplateId { get; set; }
    public DecoTemplate DecoTemplate { get; set; }
    public int Index { get; set; }
    public ICollection<DayOfWeek> DaysOfWeek { get; set; }
    public ICollection<int> DaysOfMonth { get; set; }
    public ICollection<int> MonthsOfYear { get; set; }
    public bool LimitToDateRange { get; set; }
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }

    public DateTime DateUpdated { get; set; }
    //public ICollection<DateTimeOffset> AdditionalDays { get; set; }

    // placeholder data; migration will be added later
    public int? StartYear => null;
    public int? EndYear => null;
}
