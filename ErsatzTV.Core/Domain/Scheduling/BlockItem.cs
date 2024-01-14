namespace ErsatzTV.Core.Domain.Scheduling;

public class BlockItem
{
    public int Id { get; set; }
    public int Index { get; set; }
    public int BlockId { get; set; }
    public Block Block { get; set; }
    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
    public PlaybackOrder PlaybackOrder { get; set; }
}
