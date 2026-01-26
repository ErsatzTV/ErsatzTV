using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutSkipToItemHandler(EnumeratorCache enumeratorCache) : IYamlPlayoutHandler
{
    public bool Reset => true;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<SequentialPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutSkipToItemInstruction skipToItem)
        {
            return false;
        }

        if (skipToItem.Season < 0 || skipToItem.Episode < 1)
        {
            logger.LogWarning(
                "Unable to skip to invalid season/episode: {Season}/{Episode}",
                skipToItem.Season,
                skipToItem.Episode);

            return false;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await enumeratorCache.GetCachedEnumeratorForContent(
            context,
            skipToItem.Content,
            cancellationToken);

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            var done = false;

            int enumeratorCount = enumerator is PlaylistEnumerator playlistEnumerator
                ? playlistEnumerator.CountForFiller
                : enumerator.Count;

            for (var index = 0; index < enumeratorCount; index++)
            {
                if (done)
                {
                    break;
                }

                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    if (mediaItem is Episode episode)
                    {
                        if (episode.Season?.SeasonNumber == skipToItem.Season
                            && episode.EpisodeMetadata.HeadOrNone().Map(em => em.EpisodeNumber) == skipToItem.Episode)
                        {
                            done = true;
                            break;
                        }
                    }

                    enumerator.MoveNext(Option<DateTimeOffset>.None);
                }
            }
        }

        return true;
    }
}
