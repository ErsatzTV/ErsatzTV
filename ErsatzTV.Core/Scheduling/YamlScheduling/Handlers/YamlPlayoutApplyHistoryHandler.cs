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
            if (enumerator is PlaylistEnumerator playlistEnumerator)
            {
                Option<PlayoutHistory> maybePrimaryHistory = maybeHistory
                    .Filter(h => string.IsNullOrWhiteSpace(h.ChildKey))
                    .HeadOrNone();

                foreach (PlayoutHistory primaryHistory in maybePrimaryHistory)
                {
                    var hasSetEnumeratorIndex = false;

                    var childEnumeratorKeys = playlistEnumerator.ChildEnumerators.Map(x => x.CollectionKey).ToList();
                    foreach ((IMediaCollectionEnumerator childEnumerator, CollectionKey collectionKey) in playlistEnumerator.ChildEnumerators)
                    {
                        PlaybackOrder itemPlaybackOrder = childEnumerator switch
                        {
                            ChronologicalMediaCollectionEnumerator => PlaybackOrder.Chronological,
                            RandomizedMediaCollectionEnumerator => PlaybackOrder.Random,
                            ShuffledMediaCollectionEnumerator => PlaybackOrder.Shuffle,
                            _ => PlaybackOrder.None
                        };

                        Option<PlayoutHistory> maybeApplicableHistory = maybeHistory
                            .Filter(h => h.ChildKey == HistoryDetails.KeyForCollectionKey(collectionKey))
                            .HeadOrNone();

                        List<MediaItem> collectionItems =
                            enumeratorCache.PlaylistMediaItemsForContent(contentItem.Key, collectionKey);

                        foreach (PlayoutHistory h in maybeApplicableHistory)
                        {
                            // logger.LogDebug(
                            //     "History is applicable: {When}: {ChildKey} / {History} / {IsCurrentChild}",
                            //     h.When,
                            //     h.ChildKey,
                            //     h.Details,
                            //     h.IsCurrentChild);

                            enumerator.ResetState(new CollectionEnumeratorState
                            {
                                Seed = enumerator.State.Seed,
                                Index = h.Index + (h.IsCurrentChild ? 1 : 0)
                            });

                            if (itemPlaybackOrder is PlaybackOrder.Chronological)
                            {
                                HistoryDetails.MoveToNextItem(
                                    collectionItems,
                                    h.Details,
                                    childEnumerator,
                                    itemPlaybackOrder,
                                    true);
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

                    // only move next at the end, because that may also move
                    // the enumerator index
                    playlistEnumerator.MoveNext();
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
