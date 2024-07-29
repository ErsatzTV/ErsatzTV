using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public abstract class YamlPlayoutContentHandler(EnumeratorCache enumeratorCache) : IYamlPlayoutHandler
{
    public bool Reset => false;

    public abstract Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken);

    protected async Task<Option<IMediaCollectionEnumerator>> GetContentEnumerator(
        YamlPlayoutContext context,
        string contentKey,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentKey))
        {
            return Option<IMediaCollectionEnumerator>.None;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await enumeratorCache.GetCachedEnumeratorForContent(
            context,
            contentKey,
            cancellationToken);

        if (maybeEnumerator.IsNone)
        {
            if (!enumeratorCache.MissingContentKeys.Contains(contentKey))
            {
                logger.LogWarning("Unable to locate content with key {Key}", contentKey);
                enumeratorCache.MissingContentKeys.Add(contentKey);
            }
        }

        return maybeEnumerator;
    }

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
