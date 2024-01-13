namespace ErsatzTV.Core.Domain.Scheduling;

public class TemplateItem
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public Template Template { get; set; }
    public int BlockId { get; set; }
    public Block Block { get; set; }
    public TimeSpan StartTime { get; set; }
}
