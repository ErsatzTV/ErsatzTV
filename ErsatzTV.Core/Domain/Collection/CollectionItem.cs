namespace ErsatzTV.Core.Domain;

public class CollectionItem
{
    public int CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? CustomIndex { get; set; }
}
