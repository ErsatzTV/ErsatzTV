using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Metadata;
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
    ICollectionEtag collectionEtag,
    ILogger<BlockPlayoutBuilder> logger)
    : IBlockPlayoutBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    public virtual async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        Logger.LogDebug(
            "Building block playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
            playout.Id,
            playout.Channel.Number,
            playout.Channel.Name);

        List<PlaybackOrder> allowedPlaybackOrders =
        [
            PlaybackOrder.Chronological,
            PlaybackOrder.SeasonEpisode,
            PlaybackOrder.Shuffle,
            PlaybackOrder.Random
        ];

        DateTimeOffset start = DateTimeOffset.Now;

        int daysToBuild = await GetDaysToBuild();

        // get blocks to schedule
        List<EffectiveBlock> blocksToSchedule =
            EffectiveBlock.GetEffectiveBlocks(playout.Templates, start, daysToBuild);

        // get all collection items for the playout
        Map<CollectionKey, List<MediaItem>> collectionMediaItems = await GetCollectionMediaItems(blocksToSchedule);
        Map<CollectionKey, string> collectionEtags = GetCollectionEtags(collectionMediaItems);

        Dictionary<PlayoutItem, BlockKey> itemBlockKeys =
            BlockPlayoutChangeDetection.GetPlayoutItemToBlockKeyMap(playout);

        // remove items without a block key (shouldn't happen often, just upgrades)
        playout.Items.RemoveAll(i => !itemBlockKeys.ContainsKey(i));
        
        // remove old items
        // importantly, this should not remove their history
        playout.Items.RemoveAll(i => i.FinishOffset < start);

        (List<EffectiveBlock> updatedEffectiveBlocks, List<PlayoutItem> playoutItemsToRemove) =
            BlockPlayoutChangeDetection.FindUpdatedItems(
                playout.Items,
                itemBlockKeys,
                blocksToSchedule,
                collectionEtags);

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

                Logger.LogDebug(
                    "Will schedule block {Block} at {Start}",
                    effectiveBlock.Block.Name,
                    effectiveBlock.Start);
            }
            else
            {
                Logger.LogDebug(
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
                    Logger.LogDebug(
                        "Current time {Time} for block {Block} is beyond block finish {Finish}; will stop with this block's items",
                        currentTime,
                        effectiveBlock.Block.Name,
                        blockFinish);

                    break;
                }

                // check for playout history for this collection
                string historyKey = HistoryDetails.KeyForBlockItem(blockItem);
                //logger.LogDebug("History key for block item {Item} is {Key}", blockItem.Id, historyKey);

                IMediaCollectionEnumerator enumerator = GetEnumerator(
                    playout,
                    blockItem,
                    currentTime,
                    historyKey,
                    collectionMediaItems);

                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    Logger.LogDebug(
                        "current item: {Id} / {Title}",
                        mediaItem.Id,
                        mediaItem is Episode e ? GetTitle(e) : string.Empty);

                    TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                    var collectionKey = CollectionKey.ForBlockItem(blockItem);
                    
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
                        BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                        CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                        CollectionEtag = collectionEtags[collectionKey]
                    };

                    if (effectiveBlock.Block.StopScheduling is BlockStopScheduling.BeforeDurationEnd
                        && playoutItem.FinishOffset > blockFinish)
                    {
                        Logger.LogDebug(
                            "Current time {Time} for block {Block} would go beyond block finish {Finish}; will not schedule more items",
                            currentTime,
                            effectiveBlock.Block.Name,
                            blockFinish);

                        break;
                    }

                    playout.Items.Add(playoutItem);

                    // create a playout history record
                    var nextHistory = new PlayoutHistory
                    {
                        PlayoutId = playout.Id,
                        BlockId = blockItem.BlockId,
                        PlaybackOrder = blockItem.PlaybackOrder,
                        Seed = enumerator.State.Seed,
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

    protected virtual ILogger Logger => logger;

    protected virtual async Task<int> GetDaysToBuild()
    {
        return await configElementRepository
            .GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);
    }

    protected virtual IMediaCollectionEnumerator GetEnumerator(
        Playout playout,
        BlockItem blockItem,
        DateTimeOffset currentTime,
        string historyKey,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems)
    {
        var collectionKey = CollectionKey.ForBlockItem(blockItem);
        List<MediaItem> collectionItems = collectionMediaItems[collectionKey];

        // get enumerator
        IMediaCollectionEnumerator enumerator = blockItem.PlaybackOrder switch
        {
            PlaybackOrder.Chronological => BlockPlayoutEnumerator.Chronological(
                collectionItems,
                currentTime,
                playout,
                blockItem,
                historyKey,
                Logger),
            PlaybackOrder.SeasonEpisode => BlockPlayoutEnumerator.SeasonEpisode(
                collectionItems,
                currentTime,
                playout,
                blockItem,
                historyKey,
                Logger),
            PlaybackOrder.Shuffle => BlockPlayoutEnumerator.Shuffle(
                collectionItems,
                currentTime,
                playout,
                blockItem,
                historyKey),
            _ => new RandomizedMediaCollectionEnumerator(
                collectionItems,
                new CollectionEnumeratorState { Seed = new Random().Next(), Index = 0 })
        };

        return enumerator;
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

        foreach ((string _, List<PlayoutHistory> group) in groups)
        {
            //logger.LogDebug("History key {Key} has {Count} items in group", key, group.Count);

            IEnumerable<PlayoutHistory> toDelete = group
                .Filter(h => h.When < start.UtcDateTime)
                .OrderByDescending(h => h.When);

            // chronological and season, episode only need to keep most recent entry
            if (group.Count > 0 && group[0].PlaybackOrder is PlaybackOrder.Chronological or PlaybackOrder.SeasonEpisode)
            {
                toDelete = toDelete.Tail();
            }

            // shuffle needs to keep all entries with current seed
            if (group.Count > 0 && group[0].PlaybackOrder is PlaybackOrder.Shuffle)
            {
                int currentSeed = group[0].Seed;
                toDelete = toDelete.Filter(h => h.Seed != currentSeed);
            }

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

    private Map<CollectionKey, string> GetCollectionEtags(
        Map<CollectionKey, List<MediaItem>> collectionMediaItems)
    {
        var result = new Map<CollectionKey, string>();

        foreach ((CollectionKey key, List<MediaItem> items) in collectionMediaItems)
        {
            result = result.Add(key, collectionEtag.ForCollectionItems(items));
        }

        return result;
    }

    private static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }
}
