using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public abstract class YamlPlayoutScheduler
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

    protected static FillerKind GetFillerKind(YamlPlayoutInstruction instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction.FillerKind))
        {
            return FillerKind.None;
        }

        return Enum.TryParse(instruction.FillerKind, ignoreCase: true, out FillerKind result)
            ? result
            : FillerKind.None;
    }
}
