using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool UseCustomPlaybackOrder { get; set; }
    public List<MediaItem> MediaItems { get; set; }
    public List<CollectionItem> CollectionItems { get; set; }
    public List<MultiCollection> MultiCollections { get; set; }
    public List<MultiCollectionItem> MultiCollectionItems { get; set; }
}
