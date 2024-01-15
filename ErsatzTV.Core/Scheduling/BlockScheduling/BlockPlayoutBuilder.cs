using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

public class BlockPlayoutBuilder(
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    ITelevisionRepository televisionRepository,
    IArtistRepository artistRepository,
    ILogger<BlockPlayoutBuilder> logger)
    : IBlockPlayoutBuilder
{
    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Building block playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
            playout.Id,
            playout.Channel.Number,
            playout.Channel.Name);

        List<PlaybackOrder> allowedPlaybackOrders =
        [
            PlaybackOrder.Chronological,
            PlaybackOrder.SeasonEpisode,
            PlaybackOrder.Random
        ];

        DateTimeOffset start = DateTimeOffset.Now;

        int daysToBuild = await configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

        // get blocks to schedule
        List<EffectiveBlock> blocksToSchedule = EffectiveBlock.GetEffectiveBlocks(playout, start, daysToBuild);

        // get all collection items for the playout
        Map<CollectionKey, List<MediaItem>> collectionMediaItems = await GetCollectionMediaItems(blocksToSchedule);

        Dictionary<PlayoutItem, BlockKey> itemBlockKeys = BlockPlayoutChangeDetection.GetPlayoutItemToBlockKeyMap(playout);
        
        // remove items without a block key (shouldn't happen often, just upgrades)
        playout.Items.RemoveAll(i => !itemBlockKeys.ContainsKey(i));

        (List<EffectiveBlock> updatedEffectiveBlocks, List<PlayoutItem> playoutItemsToRemove) =
            BlockPlayoutChangeDetection.FindUpdatedItems(playout, itemBlockKeys, blocksToSchedule);

        foreach (PlayoutItem playoutItem in playoutItemsToRemove)
        {
            BlockPlayoutChangeDetection.RemoveItemAndHistory(playout, playoutItem);
        }

        DateTimeOffset currentTime = start;

        foreach (EffectiveBlock effectiveBlock in updatedEffectiveBlocks)
        {
            if (currentTime < effectiveBlock.Start)
            {
                currentTime = effectiveBlock.Start;

                logger.LogDebug(
                    "Will schedule block {Block} at {Start}",
                    effectiveBlock.Block.Name,
                    effectiveBlock.Start);
            }
            else
            {
                logger.LogDebug(
                    "Will schedule block {Block} with start {Start} at {ActualStart}",
                    effectiveBlock.Block.Name,
                    effectiveBlock.Start,
                    currentTime);
            }

            DateTimeOffset blockFinish = effectiveBlock.Start.AddMinutes(effectiveBlock.Block.Minutes);

            foreach (BlockItem blockItem in effectiveBlock.Block.Items.OrderBy(i => i.Index))
            {
                // TODO: support other playback orders
                if (!allowedPlaybackOrders.Contains(blockItem.PlaybackOrder))
                {
                    continue;
                }

                if (currentTime >= blockFinish)
                {
                    logger.LogDebug(
                        "Current time {Time} for block {Block} is beyond block finish {Finish}; will stop with this block's items",
                        currentTime,
                        effectiveBlock.Block.Name,
                        blockFinish);

                    break;
                }

                // check for playout history for this collection
                string historyKey = HistoryDetails.KeyForBlockItem(blockItem);
                //logger.LogDebug("History key for block item {Item} is {Key}", blockItem.Id, historyKey);

                DateTime historyTime = currentTime.UtcDateTime;
                Option<PlayoutHistory> maybeHistory = playout.PlayoutHistory
                    .Filter(h => h.BlockId == blockItem.BlockId)
                    .Filter(h => h.Key == historyKey)
                    .Filter(h => h.When < historyTime)
                    .OrderByDescending(h => h.When)
                    .HeadOrNone();

                var state = new CollectionEnumeratorState { Seed = 0, Index = 0 };

                var collectionKey = CollectionKey.ForBlockItem(blockItem);
                List<MediaItem> collectionItems = collectionMediaItems[collectionKey];

                // get enumerator
                IMediaCollectionEnumerator enumerator = blockItem.PlaybackOrder switch
                {
                    PlaybackOrder.Chronological => new ChronologicalMediaCollectionEnumerator(collectionItems, state),
                    PlaybackOrder.SeasonEpisode => new SeasonEpisodeMediaCollectionEnumerator(collectionItems, state),
                    _ => new RandomizedMediaCollectionEnumerator(
                        collectionItems,
                        new CollectionEnumeratorState { Seed = new Random().Next(), Index = 0 })
                };

                // seek to the appropriate place in the collection enumerator
                foreach (PlayoutHistory history in maybeHistory)
                {
                    logger.LogDebug("History is applicable: {When}: {History}", history.When, history.Details);

                    HistoryDetails.MoveToNextItem(
                        collectionItems,
                        history.Details,
                        enumerator,
                        blockItem.PlaybackOrder);
                }

                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    logger.LogDebug(
                        "current item: {Id} / {Title}",
                        mediaItem.Id,
                        mediaItem is Episode e ? GetTitle(e) : string.Empty);

                    TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                    // create a playout item
                    var playoutItem = new PlayoutItem
                    {
                        MediaItemId = mediaItem.Id,
                        Start = currentTime.UtcDateTime,
                        Finish = currentTime.UtcDateTime + itemDuration,
                        InPoint = TimeSpan.Zero,
                        OutPoint = itemDuration,
                        FillerKind = FillerKind.None,
                        //CustomTitle = scheduleItem.CustomTitle,
                        //WatermarkId = scheduleItem.WatermarkId,
                        //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                        //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                        //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                        //SubtitleMode = scheduleItem.SubtitleMode
                        BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey)
                    };

                    playout.Items.Add(playoutItem);

                    // create a playout history record
                    var nextHistory = new PlayoutHistory
                    {
                        PlayoutId = playout.Id,
                        BlockId = blockItem.BlockId,
                        When = currentTime.UtcDateTime,
                        Key = historyKey,
                        Details = HistoryDetails.ForMediaItem(mediaItem)
                    };

                    //logger.LogDebug("Adding history item: {When}: {History}", nextHistory.When, nextHistory.Details);
                    playout.PlayoutHistory.Add(nextHistory);

                    currentTime += itemDuration;
                    enumerator.MoveNext();
                }
            }
        }

        CleanUpHistory(playout, start);

        return playout;
    }

    private static string GetTitle(Episode e)
    {
        string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
            .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
        var episodeNumbers = e.EpisodeMetadata.Map(em => em.EpisodeNumber).ToList();
        var episodeTitles = e.EpisodeMetadata.Map(em => em.Title).ToList();
        if (episodeNumbers.Count == 0 || episodeTitles.Count == 0)
        {
            return "[unknown episode]";
        }

        var numbersString = $"e{string.Join('e', episodeNumbers.Map(n => $"{n:00}"))}";
        var titlesString = $"{string.Join('/', episodeTitles)}";

        return $"{showTitle}s{e.Season.SeasonNumber:00}{numbersString} - {titlesString}";
    }

    private static void CleanUpHistory(Playout playout, DateTimeOffset start)
    {
        var groups = new Dictionary<string, List<PlayoutHistory>>();
        foreach (PlayoutHistory history in playout.PlayoutHistory)
        {
            var key = $"{history.BlockId}-{history.Key}";
            if (!groups.TryGetValue(key, out List<PlayoutHistory> group))
            {
                group = [];
                groups[key] = group;
            }

            group.Add(history);
        }

        foreach ((string key, List<PlayoutHistory> group) in groups)
        {
            //logger.LogDebug("History key {Key} has {Count} items in group", key, group.Count);

            IEnumerable<PlayoutHistory> toDelete = group
                .Filter(h => h.When < start.UtcDateTime)
                .OrderByDescending(h => h.When)
                .Tail();

            foreach (PlayoutHistory delete in toDelete)
            {
                playout.PlayoutHistory.Remove(delete);
            }
        }
    }

    private async Task<Map<CollectionKey, List<MediaItem>>> GetCollectionMediaItems(
        List<EffectiveBlock> effectiveBlocks)
    {
        var collectionKeys = effectiveBlocks.Map(b => b.Block.Items)
            .Flatten()
            .DistinctBy(i => i.Id)
            .Map(CollectionKey.ForBlockItem)
            .Distinct()
            .ToList();

        IEnumerable<Tuple<CollectionKey, List<MediaItem>>> tuples = await collectionKeys.Map(
            async collectionKey => Tuple(
                collectionKey,
                await MediaItemsForCollection.Collect(
                    mediaCollectionRepository,
                    televisionRepository,
                    artistRepository,
                    collectionKey))).SequenceParallel();

        return LanguageExt.Map.createRange(tuples);
    }

    private static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }
}
