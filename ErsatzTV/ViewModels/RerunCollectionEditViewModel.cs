using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.ViewModels;

public class RerunCollectionEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public CollectionType CollectionType { get; set; }
    public MediaCollectionViewModel Collection { get; set; }
    public MultiCollectionViewModel MultiCollection { get; set; }
    public SmartCollectionViewModel SmartCollection { get; set; }
    public NamedMediaItemViewModel MediaItem { get; set; }
    public PlaybackOrder FirstRunPlaybackOrder { get; set; }
    public PlaybackOrder RerunPlaybackOrder { get; set; }

    public UpdateRerunCollection ToUpdate() => new(
        Id,
        Name,
        CollectionType,
        Collection,
        MultiCollection,
        SmartCollection,
        MediaItem,
        FirstRunPlaybackOrder,
        RerunPlaybackOrder);

    public CreateRerunCollection ToCreate() => new(
        Name,
        CollectionType,
        Collection,
        MultiCollection,
        SmartCollection,
        MediaItem,
        FirstRunPlaybackOrder,
        RerunPlaybackOrder);
}
