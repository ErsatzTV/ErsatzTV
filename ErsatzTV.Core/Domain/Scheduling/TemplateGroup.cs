namespace ErsatzTV.Core.Domain.Scheduling;

public class TemplateGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Template> Templates { get; set; }
}
