using System.Collections.Immutable;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

public static class BlockPlayoutEnumerator
{
    public static IMediaCollectionEnumerator Chronological(
        List<MediaItem> collectionItems,
        DateTimeOffset currentTime,
        Playout playout,
        BlockItem blockItem,
        string historyKey,
        ILogger logger)
    {
        DateTime historyTime = currentTime.UtcDateTime;
        Option<PlayoutHistory> maybeHistory = playout.PlayoutHistory
            .Filter(h => h.BlockId == blockItem.BlockId)
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .OrderByDescending(h => h.When)
            .HeadOrNone();

        var state = new CollectionEnumeratorState { Seed = 0, Index = 0 };

        var enumerator = new ChronologicalMediaCollectionEnumerator(collectionItems, state);
        
        // seek to the appropriate place in the collection enumerator
        foreach (PlayoutHistory h in maybeHistory)
        {
            logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

            HistoryDetails.MoveToNextItem(
                collectionItems,
                h.Details,
                enumerator,
                blockItem.PlaybackOrder);
        }

        return enumerator;
    }
    
    public static IMediaCollectionEnumerator SeasonEpisode(
        List<MediaItem> collectionItems,
        DateTimeOffset currentTime,
        Playout playout,
        BlockItem blockItem,
        string historyKey,
        ILogger logger)
    {
        DateTime historyTime = currentTime.UtcDateTime;
        Option<PlayoutHistory> maybeHistory = playout.PlayoutHistory
            .Filter(h => h.BlockId == blockItem.BlockId)
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .OrderByDescending(h => h.When)
            .HeadOrNone();

        var state = new CollectionEnumeratorState { Seed = 0, Index = 0 };

        var enumerator = new SeasonEpisodeMediaCollectionEnumerator(collectionItems, state);
        
        // seek to the appropriate place in the collection enumerator
        foreach (PlayoutHistory h in maybeHistory)
        {
            logger.LogDebug("History is applicable: {When}: {History}", h.When, h.Details);

            HistoryDetails.MoveToNextItem(
                collectionItems,
                h.Details,
                enumerator,
                blockItem.PlaybackOrder);
        }

        return enumerator;
    }
    
    public static IMediaCollectionEnumerator Shuffle(
        List<MediaItem> collectionItems,
        DateTimeOffset currentTime,
        Playout playout,
        BlockItem blockItem,
        string historyKey)
    {
        DateTime historyTime = currentTime.UtcDateTime;
        Option<PlayoutHistory> maybeHistory = playout.PlayoutHistory
            .Filter(h => h.BlockId == blockItem.BlockId)
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .OrderByDescending(h => h.When)
            .HeadOrNone();

        var state = new CollectionEnumeratorState { Seed = playout.Id, Index = 0 };
        foreach (PlayoutHistory h in maybeHistory)
        {
            state.Index = h.Index + 1;
        }

        // TODO: fix multi-collection groups, keep multi-part episodes together
        var mediaItems = collectionItems
            .Map(mi => new GroupedMediaItem(mi, null))
            .ToList();

        // it shouldn't matter which order the remaining items are shuffled in,
        // as long as already-played items are not included
        return new BlockPlayoutShuffledMediaCollectionEnumerator(mediaItems, state);
    }
}
