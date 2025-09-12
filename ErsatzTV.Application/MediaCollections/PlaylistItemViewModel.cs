using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record PlaylistItemViewModel(
    int Id,
    int Index,
    ProgramScheduleItemCollectionType CollectionType,
    MediaCollectionViewModel Collection,
    MultiCollectionViewModel MultiCollection,
    SmartCollectionViewModel SmartCollection,
    NamedMediaItemViewModel MediaItem,
    PlaybackOrder PlaybackOrder,
    int? Count,
    bool PlayAll,
    bool IncludeInProgramGuide);
