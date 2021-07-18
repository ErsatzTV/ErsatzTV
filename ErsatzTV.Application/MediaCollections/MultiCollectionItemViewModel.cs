using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections
{
    public record MultiCollectionItemViewModel(
        int MultiCollectionId,
        MediaCollectionViewModel Collection,
        bool ScheduleAsGroup,
        PlaybackOrder PlaybackOrder);
}
