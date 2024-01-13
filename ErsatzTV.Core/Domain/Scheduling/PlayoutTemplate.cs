namespace ErsatzTV.Core.Domain.Scheduling;

public class PlayoutTemplate
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public int TemplateId { get; set; }
    public Template Template { get; set; }
    public int Index { get; set; }
    public ICollection<DayOfWeek> DaysOfWeek { get; set; }
    public ICollection<int> MonthsOfYear { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    // TODO: ICollection<DateTimeOffset> AdditionalDays { get; set; }

}
