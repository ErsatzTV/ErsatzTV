using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record ReplacePlaylistItem(
    int Index,
    CollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    PlaybackOrder PlaybackOrder,
    int? Count,
    bool PlayAll,
    bool IncludeInProgramGuide);
