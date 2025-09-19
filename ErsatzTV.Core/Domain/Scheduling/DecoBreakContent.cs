namespace ErsatzTV.Core.Domain.Scheduling;

public class DecoBreakContent
{
    public int Id { get; set; }
    public int DecoId { get; set; }
    public Deco Deco { get; set; }
    public CollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
    public int? PlaylistId { get; set; }
    public Playlist Playlist { get; set; }
    public DecoBreakPlacement Placement { get; set; }
}
