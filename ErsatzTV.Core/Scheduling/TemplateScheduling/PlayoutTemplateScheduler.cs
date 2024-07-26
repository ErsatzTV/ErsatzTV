using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public abstract class PlayoutTemplateScheduler
{
    protected static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        if (mediaItem is Image image)
        {
            return TimeSpan.FromSeconds(image.ImageMetadata.Head().DurationSeconds ?? Image.DefaultSeconds);
        }

        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }
}
