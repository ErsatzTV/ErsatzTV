using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record MultiCollectionSmartItemViewModel(
    int MultiCollectionId,
    SmartCollectionViewModel SmartCollection,
    bool ScheduleAsGroup,
    PlaybackOrder PlaybackOrder);