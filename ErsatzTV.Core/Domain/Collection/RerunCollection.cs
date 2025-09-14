using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class RerunCollection
{
    public int Id { get; set; }
    public string Name { get; set; }
    public CollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
    public PlaybackOrder FirstRunPlaybackOrder { get; set; }
    public PlaybackOrder RerunPlaybackOrder { get; set; }
}
