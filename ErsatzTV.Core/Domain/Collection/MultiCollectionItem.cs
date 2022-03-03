namespace ErsatzTV.Core.Domain;

public class MultiCollectionItem
{
    public int MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int CollectionId { get; set; }
    public Collection Collection { get; set; }
    public bool ScheduleAsGroup { get; set; }
    public PlaybackOrder PlaybackOrder { get; set; }
}