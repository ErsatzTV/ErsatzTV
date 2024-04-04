namespace ErsatzTV.Core.Domain.Scheduling;

public class DecoGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Deco> Decos { get; set; }
}
