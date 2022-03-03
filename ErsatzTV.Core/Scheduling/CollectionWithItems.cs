using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling;

public record CollectionWithItems(
    int CollectionId,
    List<MediaItem> MediaItems,
    bool ScheduleAsGroup,
    PlaybackOrder PlaybackOrder,
    bool UseCustomOrder);