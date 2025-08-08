namespace ErsatzTV.Core.Domain;

public class PlayoutItemGraphicsElement
{
    public int PlayoutItemId { get; set; }
    public PlayoutItem PlayoutItem { get; set; }
    public int? GraphicsElementId { get; set; }
    public GraphicsElement GraphicsElement { get; set; }
}
