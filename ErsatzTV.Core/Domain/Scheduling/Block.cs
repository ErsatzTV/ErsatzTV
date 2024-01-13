namespace ErsatzTV.Core.Domain.Scheduling;

public class Block
{
    public int Id { get; set; }
    public int BlockGroupId { get; set; }
    public BlockGroup BlockGroup { get; set; }
    public string Name { get; set; }
    public int Minutes { get; set; }
    public ICollection<BlockItem> Items { get; set; }
    public ICollection<TemplateItem> TemplateItems { get; set; }
}
