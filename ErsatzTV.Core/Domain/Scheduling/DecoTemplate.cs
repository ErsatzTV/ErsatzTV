namespace ErsatzTV.Core.Domain.Scheduling;

public class DecoTemplate
{
    public int Id { get; set; }
    public int DecoTemplateGroupId { get; set; }
    public DecoTemplateGroup DecoTemplateGroup { get; set; }
    public string Name { get; set; }
    public ICollection<DecoTemplateItem> Items { get; set; }
    public ICollection<PlayoutTemplate> PlayoutTemplates { get; set; }
    public DateTime DateUpdated { get; set; }
}
