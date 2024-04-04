namespace ErsatzTV.Core.Domain.Scheduling;

public class DecoTemplateGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<DecoTemplate> DecoTemplates { get; set; }
}
