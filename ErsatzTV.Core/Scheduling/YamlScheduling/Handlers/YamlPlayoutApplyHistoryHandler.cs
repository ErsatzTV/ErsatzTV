using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutApplyHistoryHandler(EnumeratorCache enumeratorCache)
{
    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutContentItem contentItem,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentItem.Key))
        {
            return false;
        }

        if (!Enum.TryParse(contentItem.Order, true, out PlaybackOrder playbackOrder))
        {
            return false;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await enumeratorCache.GetCachedEnumeratorForContent(
            context,
            contentItem.Key,
            cancellationToken);

        if (maybeEnumerator.IsNone)
        {
            return false;
        }

        // check for playout history for this content
        string historyKey = HistoryDetails.KeyForYamlContent(contentItem);

        DateTime historyTime = context.CurrentTime.UtcDateTime;
        Option<PlayoutHistory> maybeHistory = context.Playout.PlayoutHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .OrderByDescending(h => h.When)
            .HeadOrNone();

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            List<MediaItem> collectionItems = enumeratorCache.MediaItemsForContent(contentItem.Key);

            // seek to the appropriate place in the collection enumerator
            foreach (PlayoutHistory h in maybeHistory)
            {
                logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

                enumerator.ResetState(
                    new CollectionEnumeratorState { Seed = enumerator.State.Seed, Index = h.Index + 1 });

                if (playbackOrder is PlaybackOrder.Chronological)
                {
                    HistoryDetails.MoveToNextItem(
                        collectionItems,
                        h.Details,
                        enumerator,
                        playbackOrder);
                }
            }
        }

        return true;
    }
}
