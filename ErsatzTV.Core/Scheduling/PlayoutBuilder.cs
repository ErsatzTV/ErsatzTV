using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Map = LanguageExt.Map;

namespace ErsatzTV.Core.Scheduling
{
    // TODO: these tests fail on days when offset changes
    // because the change happens during the playout
    public class PlayoutBuilder : IPlayoutBuilder
    {
        private static readonly Random Random = new();
        private readonly IArtistRepository _artistRepository;
        private readonly ILogger<PlayoutBuilder> _logger;
        private readonly IConfigElementRepository _configElementRepository;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public PlayoutBuilder(
            IConfigElementRepository configElementRepository,
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository,
            IArtistRepository artistRepository,
            ILogger<PlayoutBuilder> logger)
        {
            _configElementRepository = configElementRepository;
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
            _artistRepository = artistRepository;
            _logger = logger;
        }

        public async Task<Playout> BuildPlayoutItems(Playout playout, bool rebuild = false)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Option<int> daysToBuild = await _configElementRepository.GetValue<int>(ConfigElementKey.PlayoutDaysToBuild);
            return await BuildPlayoutItems(playout, now, now.AddDays(await daysToBuild.IfNoneAsync(2)), rebuild);
        }

        public async Task<Playout> BuildPlayoutItems(
            Playout playout,
            DateTimeOffset playoutStart,
            DateTimeOffset playoutFinish,
            bool rebuild = false)
        {
            var collectionKeys = playout.ProgramSchedule.Items
                .Map(CollectionKeyForItem)
                .Distinct()
                .ToList();

            if (!collectionKeys.Any())
            {
                _logger.LogWarning(
                    "Playout {Playout} schedule {Schedule} has no items",
                    playout.Channel.Name,
                    playout.ProgramSchedule.Name);
                return playout;
            }

            IEnumerable<Tuple<CollectionKey, List<MediaItem>>> tuples = await collectionKeys.Map(
                async collectionKey =>
                {
                    switch (collectionKey.CollectionType)
                    {
                        case ProgramScheduleItemCollectionType.Collection:
                            List<MediaItem> collectionItems =
                                await _mediaCollectionRepository.GetItems(collectionKey.CollectionId ?? 0);
                            return Tuple(collectionKey, collectionItems);
                        case ProgramScheduleItemCollectionType.TelevisionShow:
                            List<Episode> showItems =
                                await _televisionRepository.GetShowItems(collectionKey.MediaItemId ?? 0);
                            return Tuple(collectionKey, showItems.Cast<MediaItem>().ToList());
                        case ProgramScheduleItemCollectionType.TelevisionSeason:
                            List<Episode> seasonItems =
                                await _televisionRepository.GetSeasonItems(collectionKey.MediaItemId ?? 0);
                            return Tuple(collectionKey, seasonItems.Cast<MediaItem>().ToList());
                        case ProgramScheduleItemCollectionType.Artist:
                            List<MusicVideo> artistItems =
                                await _artistRepository.GetArtistItems(collectionKey.MediaItemId ?? 0);
                            return Tuple(collectionKey, artistItems.Cast<MediaItem>().ToList());
                        case ProgramScheduleItemCollectionType.MultiCollection:
                            List<MediaItem> multiCollectionItems =
                                await _mediaCollectionRepository.GetMultiCollectionItems(
                                    collectionKey.MultiCollectionId ?? 0);
                            return Tuple(collectionKey, multiCollectionItems);
                        case ProgramScheduleItemCollectionType.SmartCollection:
                            List<MediaItem> smartCollectionItems =
                                await _mediaCollectionRepository.GetSmartCollectionItems(
                                    collectionKey.SmartCollectionId ?? 0);
                            return Tuple(collectionKey, smartCollectionItems);
                        default:
                            return Tuple(collectionKey, new List<MediaItem>());
                    }
                }).Sequence();

            var collectionMediaItems = Map.createRange(tuples);

            // using IDisposable scope = _logger.BeginScope(new { PlayoutId = playout.Id });
            _logger.LogDebug(
                $"{(rebuild ? "Rebuilding" : "Building")} playout {{PlayoutId}} for channel {{ChannelNumber}} - {{ChannelName}}",
                playout.Id,
                playout.Channel.Number,
                playout.Channel.Name);

            foreach ((CollectionKey _, List<MediaItem> items) in collectionMediaItems)
            {
                var zeroItems = new List<MediaItem>();

                foreach (MediaItem item in items)
                {
                    bool isZero = item switch
                    {
                        Movie m => await m.MediaVersions.Map(v => v.Duration).HeadOrNone().IfNoneAsync(TimeSpan.Zero) ==
                                   TimeSpan.Zero,
                        Episode e => await e.MediaVersions.Map(v => v.Duration).HeadOrNone()
                                         .IfNoneAsync(TimeSpan.Zero) ==
                                     TimeSpan.Zero,
                        MusicVideo mv => await mv.MediaVersions.Map(v => v.Duration).HeadOrNone()
                                             .IfNoneAsync(TimeSpan.Zero) ==
                                         TimeSpan.Zero,
                        _ => true
                    };

                    if (isZero)
                    {
                        _logger.LogWarning(
                            "Skipping media item with zero duration {MediaItem} - {MediaItemTitle}",
                            item.Id,
                            DisplayTitle(item));

                        zeroItems.Add(item);
                    }
                }

                items.RemoveAll(i => zeroItems.Contains(i));
            }

            // this guard needs to be below the place where we modify the collections (by removing zero-duration items)
            Option<CollectionKey> emptyCollection =
                collectionMediaItems.Find(c => !c.Value.Any()).Map(c => c.Key);
            if (emptyCollection.IsSome)
            {
                _logger.LogError(
                    "Unable to rebuild playout; collection {@CollectionKey} has no valid items!",
                    emptyCollection.ValueUnsafe());

                return playout;
            }

            // leaving this guard in for a while to ensure the zero item removal is working properly
            Option<CollectionKey> zeroDurationCollection = collectionMediaItems.Find(
                c => c.Value.Any(
                    mi => mi switch
                    {
                        Movie m => m.MediaVersions.HeadOrNone().Map(mv => mv.Duration).IfNone(TimeSpan.Zero) ==
                                   TimeSpan.Zero,
                        Episode e => e.MediaVersions.HeadOrNone().Map(mv => mv.Duration).IfNone(TimeSpan.Zero) ==
                                     TimeSpan.Zero,
                        MusicVideo mv => mv.MediaVersions.HeadOrNone().Map(v => v.Duration).IfNone(TimeSpan.Zero) ==
                                         TimeSpan.Zero,
                        _ => true
                    })).Map(c => c.Key);
            if (zeroDurationCollection.IsSome)
            {
                _logger.LogError(
                    "BUG: Unable to rebuild playout; collection {@CollectionKey} contains items with zero duration!",
                    zeroDurationCollection.ValueUnsafe());

                return playout;
            }

            playout.Items ??= new List<PlayoutItem>();
            playout.ProgramScheduleAnchors ??= new List<PlayoutProgramScheduleAnchor>();

            if (rebuild)
            {
                playout.Items.Clear();
                playout.Anchor = null;
                playout.ProgramScheduleAnchors.Clear();
            }

            var sortedScheduleItems =
                playout.ProgramSchedule.Items.OrderBy(i => i.Index).ToList();
            var collectionEnumerators = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();
            foreach ((CollectionKey collectionKey, List<MediaItem> mediaItems) in collectionMediaItems)
            {
                PlaybackOrder playbackOrder = sortedScheduleItems
                    .First(item => CollectionKeyForItem(item) == collectionKey)
                    .PlaybackOrder;
                IMediaCollectionEnumerator enumerator =
                    await GetMediaCollectionEnumerator(playout, collectionKey, mediaItems, playbackOrder);
                collectionEnumerators.Add(collectionKey, enumerator);
            }

            // find start anchor
            PlayoutAnchor startAnchor = FindStartAnchor(playout, playoutStart, sortedScheduleItems);

            // start at the previously-decided time
            DateTimeOffset currentTime = startAnchor.NextStartOffset.ToLocalTime();
            _logger.LogDebug(
                "Starting playout {PlayoutId} for channel {ChannelNumber} - {ChannelName} at {StartTime}",
                playout.Id,
                playout.Channel.Number,
                playout.Channel.Name,
                currentTime);

            // start with the previously-decided schedule item
            int index = sortedScheduleItems.IndexOf(startAnchor.NextScheduleItem);

            // start with the previous multiple/duration states
            Option<int> multipleRemaining = Optional(startAnchor.MultipleRemaining);
            Option<DateTimeOffset> durationFinish = startAnchor.DurationFinishOffset;
            bool inFlood = startAnchor.InFlood;

            bool customGroup = multipleRemaining.IsSome || durationFinish.IsSome;

            // loop until we're done filling the desired amount of time
            while (currentTime < playoutFinish)
            {
                // get the schedule item out of the sorted list
                ProgramScheduleItem scheduleItem = sortedScheduleItems[index % sortedScheduleItems.Count];

                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(
                    scheduleItem,
                    currentTime,
                    multipleRemaining.IsSome,
                    durationFinish.IsSome,
                    inFlood);

                IMediaCollectionEnumerator enumerator = collectionEnumerators[CollectionKeyForItem(scheduleItem)];
                await enumerator.Current.IfSomeAsync(
                    mediaItem =>
                    {
                        _logger.LogDebug(
                            "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
                            scheduleItem.Index,
                            scheduleItem.CollectionType,
                            mediaItem.Id,
                            DisplayTitle(mediaItem),
                            itemStartTime);

                        MediaVersion version = mediaItem switch
                        {
                            Movie m => m.MediaVersions.Head(),
                            Episode e => e.MediaVersions.Head(),
                            MusicVideo mv => mv.MediaVersions.Head(),
                            _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                        };

                        var playoutItem = new PlayoutItem
                        {
                            MediaItemId = mediaItem.Id,
                            Start = itemStartTime.UtcDateTime,
                            Finish = itemStartTime.UtcDateTime + version.Duration,
                            CustomGroup = customGroup
                        };

                        if (!string.IsNullOrWhiteSpace(scheduleItem.CustomTitle))
                        {
                            playoutItem.CustomTitle = scheduleItem.CustomTitle;
                        }

                        currentTime = itemStartTime + version.Duration;
                        enumerator.MoveNext();

                        playout.Items.Add(playoutItem);

                        switch (scheduleItem)
                        {
                            case ProgramScheduleItemOne:
                                // only play one item from collection, so always advance to the next item
                                _logger.LogDebug(
                                    "Advancing to next schedule item after playout mode {PlayoutMode}",
                                    "One");
                                index++;
                                customGroup = false;
                                break;
                            case ProgramScheduleItemMultiple multiple:
                                if (multipleRemaining.IsNone)
                                {
                                    if (multiple.Count == 0)
                                    {
                                        multipleRemaining = collectionMediaItems[CollectionKeyForItem(scheduleItem)]
                                            .Count;
                                    }
                                    else
                                    {
                                        multipleRemaining = multiple.Count;
                                    }

                                    customGroup = true;
                                }

                                multipleRemaining = multipleRemaining.Map(i => i - 1);
                                if (multipleRemaining.IfNone(-1) == 0)
                                {
                                    _logger.LogDebug(
                                        "Advancing to next schedule item after playout mode {PlayoutMode}",
                                        "Multiple");
                                    index++;
                                    multipleRemaining = None;
                                    customGroup = false;
                                }

                                break;
                            case ProgramScheduleItemFlood:
                                enumerator.Current.Do(
                                    peekMediaItem =>
                                    {
                                        customGroup = true;

                                        MediaVersion peekVersion = peekMediaItem switch
                                        {
                                            Movie m => m.MediaVersions.Head(),
                                            Episode e => e.MediaVersions.Head(),
                                            MusicVideo mv => mv.MediaVersions.Head(),
                                            _ => throw new ArgumentOutOfRangeException(nameof(peekMediaItem))
                                        };

                                        ProgramScheduleItem peekScheduleItem =
                                            sortedScheduleItems[(index + 1) % sortedScheduleItems.Count];
                                        DateTimeOffset peekScheduleItemStart =
                                            peekScheduleItem.StartType == StartType.Fixed
                                                ? GetStartTimeAfter(peekScheduleItem, currentTime)
                                                : DateTimeOffset.MaxValue;

                                        // if the current time is before the next schedule item, but the current finish
                                        // is after, we need to move on to the next schedule item
                                        // eventually, spots probably have to fit in this gap
                                        bool willNotFinishInTime = currentTime <= peekScheduleItemStart &&
                                                                   currentTime + peekVersion.Duration >
                                                                   peekScheduleItemStart;
                                        if (willNotFinishInTime)
                                        {
                                            _logger.LogDebug(
                                                "Advancing to next schedule item after playout mode {PlayoutMode}",
                                                "Flood");
                                            index++;
                                            customGroup = false;
                                            inFlood = false;
                                        }
                                        else
                                        {
                                            inFlood = true;
                                        }
                                    });
                                break;
                            case ProgramScheduleItemDuration duration:
                                enumerator.Current.Do(
                                    peekMediaItem =>
                                    {
                                        MediaVersion peekVersion = peekMediaItem switch
                                        {
                                            Movie m => m.MediaVersions.Head(),
                                            Episode e => e.MediaVersions.Head(),
                                            MusicVideo mv => mv.MediaVersions.Head(),
                                            _ => throw new ArgumentOutOfRangeException(nameof(peekMediaItem))
                                        };

                                        // remember when we need to finish this duration item
                                        if (durationFinish.IsNone)
                                        {
                                            durationFinish = itemStartTime + duration.PlayoutDuration;
                                            customGroup = true;
                                        }

                                        bool willNotFinishInTime =
                                            currentTime <= durationFinish.IfNone(DateTime.MinValue) &&
                                            currentTime + peekVersion.Duration >
                                            durationFinish.IfNone(DateTime.MinValue);
                                        if (willNotFinishInTime)
                                        {
                                            _logger.LogDebug(
                                                "Advancing to next schedule item after playout mode {PlayoutMode}",
                                                "Duration");
                                            index++;
                                            customGroup = false;

                                            if (duration.OfflineTail)
                                            {
                                                durationFinish.Do(f => currentTime = f);
                                            }

                                            durationFinish = None;
                                        }
                                    }
                                );
                                break;
                        }
                    });
            }

            // once more to get playout anchor
            ProgramScheduleItem nextScheduleItem = sortedScheduleItems[index % sortedScheduleItems.Count];

            // build program schedule anchors
            playout.ProgramScheduleAnchors = BuildProgramScheduleAnchors(playout, collectionEnumerators);

            // remove any items outside the desired range
            playout.Items.RemoveAll(old => old.FinishOffset < playoutStart || old.StartOffset > playoutFinish);

            DateTimeOffset minCurrentTime = currentTime;
            if (playout.Items.Any())
            {
                DateTimeOffset maxStartTime = playout.Items.Max(i => i.FinishOffset);
                if (maxStartTime < currentTime)
                {
                    minCurrentTime = maxStartTime;
                }
            }
            
            playout.Anchor = new PlayoutAnchor
            {
                NextScheduleItem = nextScheduleItem,
                NextScheduleItemId = nextScheduleItem.Id,
                NextStart = GetStartTimeAfter(nextScheduleItem, minCurrentTime).UtcDateTime,
                MultipleRemaining = multipleRemaining.IsSome ? multipleRemaining.ValueUnsafe() : null,
                DurationFinish = durationFinish.IsSome ? durationFinish.ValueUnsafe().UtcDateTime : null,
                InFlood = inFlood
            };

            return playout;
        }

        private static PlayoutAnchor FindStartAnchor(
            Playout playout,
            DateTimeOffset start,
            IReadOnlyCollection<ProgramScheduleItem> sortedScheduleItems) =>
            Optional(playout.Anchor).IfNone(
                () =>
                {
                    ProgramScheduleItem schedule = sortedScheduleItems.Head();
                    switch (schedule.StartType)
                    {
                        case StartType.Fixed:
                            return new PlayoutAnchor
                            {
                                NextScheduleItem = schedule,
                                NextScheduleItemId = schedule.Id,
                                NextStart = (start - start.TimeOfDay).UtcDateTime +
                                            schedule.StartTime.GetValueOrDefault()
                            };
                        case StartType.Dynamic:
                        default:
                            return new PlayoutAnchor
                            {
                                NextScheduleItem = schedule,
                                NextScheduleItemId = schedule.Id,
                                NextStart = (start - start.TimeOfDay).UtcDateTime
                            };
                    }
                });

        private static DateTimeOffset GetStartTimeAfter(
            ProgramScheduleItem item,
            DateTimeOffset start,
            bool inMultiple = false,
            bool inDuration = false,
            bool inFlood = false)
        {
            switch (item.StartType)
            {
                case StartType.Fixed:
                    if (item is ProgramScheduleItemMultiple && inMultiple ||
                        item is ProgramScheduleItemDuration && inDuration ||
                        item is ProgramScheduleItemFlood && inFlood)
                    {
                        return start;
                    }

                    TimeSpan startTime = item.StartTime.GetValueOrDefault();
                    DateTimeOffset result = start.Date + startTime;
                    // need to wrap to the next day if appropriate
                    return start.TimeOfDay > startTime ? result.AddDays(1) : result;
                case StartType.Dynamic:
                default:
                    return start;
            }
        }

        private static List<PlayoutProgramScheduleAnchor> BuildProgramScheduleAnchors(
            Playout playout,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators)
        {
            var result = new List<PlayoutProgramScheduleAnchor>();

            foreach (CollectionKey collectionKey in collectionEnumerators.Keys)
            {
                Option<PlayoutProgramScheduleAnchor> maybeExisting = playout.ProgramScheduleAnchors.FirstOrDefault(
                    a => a.CollectionType == collectionKey.CollectionType
                         && a.CollectionId == collectionKey.CollectionId
                         && a.MediaItemId == collectionKey.MediaItemId);

                var maybeEnumeratorState = collectionEnumerators.GroupBy(e => e.Key, e => e.Value.State).ToDictionary(
                    mcs => mcs.Key,
                    mcs => mcs.Head());

                PlayoutProgramScheduleAnchor scheduleAnchor = maybeExisting.Match(
                    existing =>
                    {
                        existing.EnumeratorState = maybeEnumeratorState[collectionKey];
                        return existing;
                    },
                    () => new PlayoutProgramScheduleAnchor
                    {
                        Playout = playout,
                        PlayoutId = playout.Id,
                        ProgramSchedule = playout.ProgramSchedule,
                        ProgramScheduleId = playout.ProgramScheduleId,
                        CollectionType = collectionKey.CollectionType,
                        CollectionId = collectionKey.CollectionId,
                        MultiCollectionId = collectionKey.MultiCollectionId,
                        SmartCollectionId = collectionKey.SmartCollectionId,
                        MediaItemId = collectionKey.MediaItemId,
                        EnumeratorState = maybeEnumeratorState[collectionKey]
                    });

                result.Add(scheduleAnchor);
            }

            return result;
        }

        private async Task<IMediaCollectionEnumerator> GetMediaCollectionEnumerator(
            Playout playout,
            CollectionKey collectionKey,
            List<MediaItem> mediaItems,
            PlaybackOrder playbackOrder)
        {
            Option<PlayoutProgramScheduleAnchor> maybeAnchor = playout.ProgramScheduleAnchors.FirstOrDefault(
                a => a.ProgramScheduleId == playout.ProgramScheduleId
                     && a.CollectionType == collectionKey.CollectionType
                     && a.CollectionId == collectionKey.CollectionId
                     && a.MultiCollectionId == collectionKey.MultiCollectionId
                     && a.SmartCollectionId == collectionKey.SmartCollectionId
                     && a.MediaItemId == collectionKey.MediaItemId);

            CollectionEnumeratorState state = maybeAnchor.Match(
                anchor => anchor.EnumeratorState,
                () => new CollectionEnumeratorState { Seed = Random.Next(), Index = 0 });

            if (await _mediaCollectionRepository.IsCustomPlaybackOrder(collectionKey.CollectionId ?? 0))
            {
                Option<Collection> collectionWithItems =
                    await _mediaCollectionRepository.GetCollectionWithCollectionItemsUntracked(
                        collectionKey.CollectionId ?? 0);

                if (collectionKey.CollectionType == ProgramScheduleItemCollectionType.Collection &&
                    collectionWithItems.IsSome)
                {
                    return new CustomOrderCollectionEnumerator(
                        collectionWithItems.ValueUnsafe(),
                        mediaItems,
                        state);
                }
            }
            
            switch (playbackOrder)
            {
                case PlaybackOrder.Chronological:
                    return new ChronologicalMediaCollectionEnumerator(mediaItems, state);
                case PlaybackOrder.Random:
                    return new RandomizedMediaCollectionEnumerator(mediaItems, state);
                case PlaybackOrder.Shuffle:
                    return new ShuffledMediaCollectionEnumerator(
                        await GetGroupedMediaItemsForShuffle(playout, mediaItems, collectionKey),
                        state);
                case PlaybackOrder.ShuffleInOrder:
                    return new ShuffleInOrderCollectionEnumerator(
                        await GetCollectionItemsForShuffleInOrder(collectionKey),
                        state);
                default:
                    // TODO: handle this error case differently?
                    return new RandomizedMediaCollectionEnumerator(mediaItems, state);
            }
        }

        private async Task<List<GroupedMediaItem>> GetGroupedMediaItemsForShuffle(
            Playout playout,
            List<MediaItem> mediaItems,
            CollectionKey collectionKey)
        {
            if (collectionKey.MultiCollectionId != null)
            {
                List<CollectionWithItems> collections = await _mediaCollectionRepository
                    .GetMultiCollectionCollections(collectionKey.MultiCollectionId.Value);

                return MultiCollectionGrouper.GroupMediaItems(collections);
            }

            return playout.ProgramSchedule.KeepMultiPartEpisodesTogether
                ? MultiPartEpisodeGrouper.GroupMediaItems(
                    mediaItems,
                    playout.ProgramSchedule.TreatCollectionsAsShows)
                : mediaItems.Map(mi => new GroupedMediaItem(mi, null)).ToList();
        }

        private async Task<List<CollectionWithItems>> GetCollectionItemsForShuffleInOrder(CollectionKey collectionKey)
        {
            var result = new List<CollectionWithItems>();

            if (collectionKey.MultiCollectionId != null)
            {
                result = await _mediaCollectionRepository.GetMultiCollectionCollections(
                    collectionKey.MultiCollectionId.Value);
            }

            return result;
        }

        private static string DisplayTitle(MediaItem mediaItem)
        {
            switch (mediaItem)
            {
                case Episode e:
                    string showTitle = e.Season.Show.ShowMetadata.HeadOrNone()
                        .Map(sm => $"{sm.Title} - ").IfNone(string.Empty);
                    return e.EpisodeMetadata.HeadOrNone()
                        .Map(em => $"{showTitle}s{e.Season.SeasonNumber:00}e{em.EpisodeNumber:00} - {em.Title}")
                        .IfNone("[unknown episode]");
                case Movie m:
                    return m.MovieMetadata.HeadOrNone().Match(mm => mm.Title ?? string.Empty, () => "[unknown movie]");
                case MusicVideo mv:
                    string artistName = mv.Artist.ArtistMetadata.HeadOrNone()
                        .Map(am => $"{am.Title} - ").IfNone(string.Empty);
                    return mv.MusicVideoMetadata.HeadOrNone()
                        .Map(mvm => $"{artistName}{mvm.Title}")
                        .IfNone("[unknown music video]");
                default:
                    return string.Empty;
            }
        }

        private static CollectionKey CollectionKeyForItem(ProgramScheduleItem item) =>
            item.CollectionType switch
            {
                ProgramScheduleItemCollectionType.Collection => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    CollectionId = item.CollectionId
                },
                ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    MediaItemId = item.MediaItemId
                },
                ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    MediaItemId = item.MediaItemId
                },
                ProgramScheduleItemCollectionType.Artist => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    MediaItemId = item.MediaItemId
                },
                ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    MultiCollectionId = item.MultiCollectionId
                },
                ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    SmartCollectionId = item.SmartCollectionId
                },
                _ => throw new ArgumentOutOfRangeException(nameof(item))
            };

        private class CollectionKey : Record<CollectionKey>
        {
            public ProgramScheduleItemCollectionType CollectionType { get; set; }
            public int? CollectionId { get; set; }
            public int? MultiCollectionId { get; set; }
            public int? SmartCollectionId { get; set; }
            public int? MediaItemId { get; set; }
        }
    }
}
