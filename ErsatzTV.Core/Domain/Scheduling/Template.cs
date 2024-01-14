namespace ErsatzTV.Core.Domain.Scheduling;

public class Template
{
    public int Id { get; set; }
    public int TemplateGroupId { get; set; }
    public TemplateGroup TemplateGroup { get; set; }
    public string Name { get; set; }
    public ICollection<TemplateItem> Items { get; set; }
    public ICollection<PlayoutTemplate> PlayoutTemplates { get; set; }
}
