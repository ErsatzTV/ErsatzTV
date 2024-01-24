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
        // need a new shuffled media collection enumerator that can "hide" items for one iteration, then include all items again
        // maybe take a "masked items" hash set, then clear it after shuffling
        
        DateTime historyTime = currentTime.UtcDateTime;
        var maskedMediaItemIds = new System.Collections.Generic.HashSet<int>();
        List<PlayoutHistory> history = playout.PlayoutHistory
            .Filter(h => h.BlockId == blockItem.BlockId)
            .Filter(h => h.Key == historyKey)
            .Filter(h => h.When < historyTime)
            .OrderByDescending(h => h.When)
            .ToList();

        if (history.Count > 0)
        {
            int currentSeed = history[0].Seed;
            history = history.Filter(h => h.Seed == currentSeed).ToList();
        }

        var knownMediaIds = collectionItems.Map(ci => ci.Id).ToImmutableHashSet();
        foreach (PlayoutHistory h in history)
        {
            HistoryDetails.Details details = JsonConvert.DeserializeObject<HistoryDetails.Details>(h.Details);
            foreach (int mediaItemId in Optional(details.MediaItemId))
            {
                if (knownMediaIds.Contains(mediaItemId))
                {
                    maskedMediaItemIds.Add(mediaItemId);
                }
            }
        }

        var state = new CollectionEnumeratorState { Seed = new Random().Next(), Index = 0 };

        // keep the current seed if one exists 
        if (maskedMediaItemIds.Count > 0 && maskedMediaItemIds.Count < collectionItems.Count && history.Count > 0)
        {
            state.Seed = history[0].Seed;
        }

        // if everything is masked, nothing is masked
        if (maskedMediaItemIds.Count == collectionItems.Count)
        {
            maskedMediaItemIds.Clear();
        }

        // TODO: fix multi-collection groups, keep multi-part episodes together
        var mediaItems = collectionItems
            .Map(mi => new GroupedMediaItem(mi, null))
            .ToList();

        Serilog.Log.Logger.Debug(
            "scheduling {X} media items with {Y} masked",
            mediaItems.Count,
            maskedMediaItemIds.Count);

        // it shouldn't matter which order the remaining items are shuffled in,
        // as long as already-played items are not included
        return new MaskedShuffledMediaCollectionEnumerator(
            mediaItems,
            maskedMediaItemIds,
            state,
            CancellationToken.None);
    }
}
