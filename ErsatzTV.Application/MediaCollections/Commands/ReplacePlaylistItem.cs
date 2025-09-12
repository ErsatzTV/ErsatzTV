using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record ReplacePlaylistItem(
    int Index,
    ProgramScheduleItemCollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    PlaybackOrder PlaybackOrder,
    int? Count,
    bool PlayAll,
    bool IncludeInProgramGuide);
