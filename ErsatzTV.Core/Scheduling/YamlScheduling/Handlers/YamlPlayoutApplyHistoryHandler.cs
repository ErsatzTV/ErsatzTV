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
        Option<DateTime> maxWhen = await context.Playout.PlayoutHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .Map(h => h.When)
            .OrderByDescending(h => h)
            .HeadOrNone()
            .IfNoneAsync(DateTime.MinValue);

        var maybeHistory = context.Playout.PlayoutHistory
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When == maxWhen)
            .ToList();

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            if (contentItem is YamlPlayoutContentMarathonItem marathonItem && enumerator is PlaylistEnumerator playlistEnumerator)
            {
                Option<PlayoutHistory> maybePrimaryHistory = maybeHistory
                    .Filter(h => string.IsNullOrWhiteSpace(h.ChildKey))
                    .HeadOrNone();

                foreach (PlayoutHistory primaryHistory in maybePrimaryHistory)
                {
                    bool hasSetEnumeratorIndex = false;

                    if (!Enum.TryParse(marathonItem.ItemOrder, true, out PlaybackOrder itemPlaybackOrder))
                    {
                        itemPlaybackOrder = PlaybackOrder.None;
                    }

                    var childEnumeratorKeys = playlistEnumerator.ChildEnumerators.Keys.ToList();
                    foreach ((CollectionKey collectionKey, IMediaCollectionEnumerator childEnumerator) in
                             playlistEnumerator.ChildEnumerators)
                    {
                        Option<PlayoutHistory> maybeApplicableHistory = maybeHistory
                            .Filter(h => h.ChildKey == HistoryDetails.KeyForCollectionKey(collectionKey))
                            .HeadOrNone();

                        List<MediaItem> collectionItems =
                            enumeratorCache.PlaylistMediaItemsForContent(contentItem.Key, collectionKey);

                        foreach (PlayoutHistory h in maybeApplicableHistory)
                        {
                            // logger.LogDebug(
                            //     "History is applicable: {When}: {ChildKey} / {History}",
                            //     h.When,
                            //     h.ChildKey,
                            //     h.Details);

                            enumerator.ResetState(
                                new CollectionEnumeratorState { Seed = enumerator.State.Seed, Index = h.Index + 1 });

                            if (itemPlaybackOrder is PlaybackOrder.Chronological)
                            {
                                HistoryDetails.MoveToNextItem(
                                    collectionItems,
                                    h.Details,
                                    childEnumerator,
                                    playbackOrder,
                                    h.IsCurrentChild);
                            }

                            if (h.IsCurrentChild)
                            {
                                // try to find enumerator based on collection key
                                playlistEnumerator.SetEnumeratorIndex(childEnumeratorKeys.IndexOf(collectionKey));
                                hasSetEnumeratorIndex = true;
                            }
                        }
                    }

                    if (!hasSetEnumeratorIndex)
                    {
                        // falling back to enumerator based on index
                        playlistEnumerator.SetEnumeratorIndex(primaryHistory.Index);
                    }
                }
            }
            else
            {
                List<MediaItem> collectionItems = enumeratorCache.MediaItemsForContent(contentItem.Key);

                // seek to the appropriate place in the collection enumerator
                foreach (PlayoutHistory h in maybeHistory)
                {
                    // logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

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
        }

        return true;
    }
}
