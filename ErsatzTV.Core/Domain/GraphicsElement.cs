namespace ErsatzTV.Core.Domain;

public class GraphicsElement
{
    public int Id { get; set; }
    public string Path { get; set; }
    public GraphicsElementKind Kind { get; set; }
    public List<PlayoutItem> PlayoutItems { get; set; }
    public List<PlayoutItemGraphicsElement> PlayoutItemGraphicsElements { get; set; }
    public List<ProgramScheduleItem> ProgramScheduleItems { get; set; }
    public List<ProgramScheduleItemGraphicsElement> ProgramScheduleItemGraphicsElements { get; set; }
}
