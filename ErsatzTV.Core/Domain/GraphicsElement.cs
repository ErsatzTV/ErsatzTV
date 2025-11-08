using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class GraphicsElement
{
    public int Id { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public GraphicsElementKind Kind { get; set; }
    public List<PlayoutItem> PlayoutItems { get; set; }
    public List<PlayoutItemGraphicsElement> PlayoutItemGraphicsElements { get; set; }
    public List<ProgramScheduleItem> ProgramScheduleItems { get; set; }
    public List<ProgramScheduleItemGraphicsElement> ProgramScheduleItemGraphicsElements { get; set; }
    public List<BlockItem> BlockItems { get; set; }
    public List<BlockItemGraphicsElement> BlockItemGraphicsElements { get; set; }
    public List<Deco> Decos { get; set; }
    public List<DecoGraphicsElement> DecoGraphicsElements { get; set; }

    // for unit testing
    public override string ToString() => Path;
}
