using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Scheduling;

public record ReplaceBlockItem(
    int Index,
    ProgramScheduleItemCollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    PlaybackOrder PlaybackOrder);
