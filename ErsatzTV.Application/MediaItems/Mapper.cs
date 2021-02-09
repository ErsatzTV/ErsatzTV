using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaItems
{
    internal static class Mapper
    {
        internal static MediaItemViewModel ProjectToViewModel(MediaItem mediaItem) =>
            new(
                mediaItem.Id,
                mediaItem.MediaSourceId,
                mediaItem.Path);
    }
}
