namespace ErsatzTV.Core.Domain.Scheduling;

public class BlockGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Block> Blocks { get; set; }
}
