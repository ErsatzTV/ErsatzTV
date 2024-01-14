namespace ErsatzTV.Core.Domain.Scheduling;

public class BlockGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Block> Blocks { get; set; }
}
