using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling;

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

        DateTimeOffset start = DateTimeOffset.Now;
        
        // get blocks to schedule
        List<RealBlock> blocksToSchedule = await GetBlocksToSchedule(playout, start);

        // get all collection items for the playout
        Map<CollectionKey, List<MediaItem>> collectionMediaItems = await GetCollectionMediaItems(blocksToSchedule);
        
        // TODO: REMOVE THIS !!!
        playout.Items.Clear();
        
        // TODO: REMOVE THIS !!!
        var historyToRemove = playout.PlayoutHistory
            .Filter(h => h.When > start.UtcDateTime)
            .ToList();
        foreach (PlayoutHistory remove in historyToRemove)
        {
            playout.PlayoutHistory.Remove(remove);
        }

        foreach (RealBlock realBlock in blocksToSchedule)
        {
            logger.LogDebug(
                "Will schedule block {Block} at {Start}",
                realBlock.Block.Name,
                realBlock.Start);

            DateTimeOffset currentTime = realBlock.Start;
            
            foreach (BlockItem blockItem in realBlock.Block.Items)
            {
                // TODO: support other playback orders
                if (blockItem.PlaybackOrder is not PlaybackOrder.SeasonEpisode and not PlaybackOrder.Chronological)
                {
                    continue;
                }
                
                // TODO: check if change is needed - if not, skip building
                // - block can change
                // - template can change
                // - playout templates can change
                
                // TODO: check for playout history for this collection
                string historyKey = HistoryKey.ForBlockItem(blockItem);
                logger.LogDebug("History key for block item {Item} is {Key}", blockItem.Id, historyKey);

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
                var enumerator = new SeasonEpisodeMediaCollectionEnumerator(collectionItems, state);
                
                // seek to the appropriate place in the collection enumerator
                foreach (PlayoutHistory history in maybeHistory)
                {
                    logger.LogDebug("History is applicable: {When}: {History}", history.When, history.Details);
                    
                    // find next media item
                    HistoryDetails.Details details = JsonConvert.DeserializeObject<HistoryDetails.Details>(history.Details);
                    if (details.SeasonNumber.HasValue && details.EpisodeNumber.HasValue)
                    {
                        Option<MediaItem> maybeMatchedItem = Optional(
                            collectionItems.Find(
                                ci => ci is Episode e &&
                                      e.EpisodeMetadata.Any(em => em.EpisodeNumber == details.EpisodeNumber.Value) &&
                                      e.Season.SeasonNumber == details.SeasonNumber.Value));

                        var copy = collectionItems.ToList();

                        if (maybeMatchedItem.IsNone)
                        {
                            var fakeItem = new Episode
                            {
                                Season = new Season { SeasonNumber = details.SeasonNumber.Value },
                                EpisodeMetadata =
                                [
                                    new EpisodeMetadata
                                    {
                                        EpisodeNumber = details.EpisodeNumber.Value,
                                        ReleaseDate = details.ReleaseDate
                                    }
                                ]
                            };

                            copy.Add(fakeItem);
                            maybeMatchedItem = fakeItem;
                        }

                        foreach (MediaItem matchedItem in maybeMatchedItem)
                        {
                            IComparer<MediaItem> comparer = blockItem.PlaybackOrder switch
                            {
                                PlaybackOrder.Chronological => new ChronologicalMediaComparer(),
                                _ => new SeasonEpisodeMediaComparer()
                            };

                            copy.Sort(comparer);

                            state.Index = copy.IndexOf(matchedItem);
                            enumerator.ResetState(state);
                            enumerator.MoveNext();
                        }
                    }
                }
                
                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    logger.LogDebug("current item: {Id} / {Title}", mediaItem.Id, mediaItem is Episode e ? GetTitle(e) : string.Empty);

                    TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                    // TODO: create a playout item
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
                    };

                    playout.Items.Add(playoutItem);
                    
                    // TODO: create a playout history record
                    var nextHistory = new PlayoutHistory
                    {
                        PlayoutId = playout.Id,
                        BlockId = blockItem.BlockId,
                        When = currentTime.UtcDateTime,
                        Key = historyKey,
                        Details = HistoryDetails.ForMediaItem(mediaItem)
                    };
                    
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

    private async Task<List<RealBlock>> GetBlocksToSchedule(Playout playout, DateTimeOffset start)
    {
        int daysToBuild = await configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild)
            .IfNoneAsync(2);

        DateTimeOffset finish = start.AddDays(daysToBuild);

        var realBlocks = new List<RealBlock>();
        DateTimeOffset current = start.Date;
        while (current < finish)
        {
            foreach (PlayoutTemplate playoutTemplate in PlayoutTemplateSelector.GetPlayoutTemplateFor(playout.Templates, current))
            {
                // logger.LogDebug(
                //     "Will schedule day {Date} using template {Template}",
                //     current,
                //     playoutTemplate.Template.Name);

                foreach (TemplateItem templateItem in playoutTemplate.Template.Items)
                {
                    var realBlock = new RealBlock(
                        templateItem.Block,
                        new DateTimeOffset(
                            current.Year,
                            current.Month,
                            current.Day,
                            templateItem.StartTime.Hours,
                            templateItem.StartTime.Minutes,
                            0,
                            start.Offset));

                    realBlocks.Add(realBlock);
                }
                
                current = current.AddDays(1);
            }
        }

        realBlocks.RemoveAll(b => b.Start.AddMinutes(b.Block.Minutes) < start || b.Start > finish);

        return realBlocks;
    }

    private void CleanUpHistory(Playout playout, DateTimeOffset start)
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
            logger.LogDebug("History key {Key} has {Count} items in group", key, group.Count);

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
    
    private async Task<Map<CollectionKey, List<MediaItem>>> GetCollectionMediaItems(List<RealBlock> realBlocks)
    {
        var collectionKeys = realBlocks.Map(b => b.Block.Items)
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

    private record RealBlock(Block Block, DateTimeOffset Start);

    private static class HistoryKey
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public static string ForBlockItem(BlockItem blockItem)
        {
            dynamic key = new
            {
                blockItem.BlockId,
                blockItem.PlaybackOrder,
                blockItem.CollectionType,
                blockItem.CollectionId,
                blockItem.MultiCollectionId,
                blockItem.SmartCollectionId,
                blockItem.MediaItemId
            };

            return JsonConvert.SerializeObject(key, Formatting.None, Settings);
        }
    }

    private static class HistoryDetails
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public static string ForMediaItem(MediaItem mediaItem)
        {
            Details details = mediaItem switch
            {
                Episode e => ForEpisode(e),
                _ => new Details(mediaItem.Id, null, null, null)
            };
            
            return JsonConvert.SerializeObject(details, Formatting.None, Settings);
        }

        private static Details ForEpisode(Episode e)
        {
            int? episodeNumber = null;
            DateTime? releaseDate = null;
            foreach (EpisodeMetadata episodeMetadata in e.EpisodeMetadata.HeadOrNone())
            {
                episodeNumber = episodeMetadata.EpisodeNumber;
                releaseDate = episodeMetadata.ReleaseDate;
            }

            return new Details(e.Id, releaseDate, e.Season.SeasonNumber, episodeNumber);
        }

        public record Details(int? MediaItemId, DateTime? ReleaseDate, int? SeasonNumber, int? EpisodeNumber);
    }
}
