namespace ErsatzTV.Core.Domain.Scheduling;

public class DecoTemplateItem
{
    public int Id { get; set; }
    public int DecoTemplateId { get; set; }
    public DecoTemplate DecoTemplate { get; set; }
    public int DecoId { get; set; }
    public Deco Deco { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
