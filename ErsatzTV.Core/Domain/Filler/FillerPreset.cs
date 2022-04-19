namespace ErsatzTV.Core.Domain.Filler;

public class FillerPreset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public FillerKind FillerKind { get; set; }
    public FillerMode FillerMode { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? Count { get; set; }
    public int? PadToNearestMinute { get; set; }
    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
}
