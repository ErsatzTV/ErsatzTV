﻿using System;
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
            Map<CollectionKey, List<MediaItem>> collectionMediaItems = await GetCollectionMediaItems(playout);
            if (!collectionMediaItems.Any())
            {
                _logger.LogWarning(
                    "Playout {Playout} schedule {Schedule} has no items",
                    playout.Channel.Name,
                    playout.ProgramSchedule.Name);

                return playout;
            }

            _logger.LogDebug(
                "{Action} playout {PlayoutId} for channel {ChannelNumber} - {ChannelName}",
                rebuild ? "Rebuilding" : "Building",
                playout.Id,
                playout.Channel.Number,
                playout.Channel.Name);

            Option<CollectionKey> maybeEmptyCollection = await CheckForEmptyCollections(collectionMediaItems);
            foreach (CollectionKey emptyCollection in maybeEmptyCollection)
            {
                _logger.LogError(
                    "Unable to rebuild playout; collection {@CollectionKey} has no valid items!",
                    emptyCollection);

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

            var sortedScheduleItems = playout.ProgramSchedule.Items.OrderBy(i => i.Index).ToList();
            var collectionEnumerators = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();
            foreach ((CollectionKey collectionKey, List<MediaItem> mediaItems) in collectionMediaItems)
            {
                // use configured playback order for primary collection, shuffle for filler
                Option<ProgramScheduleItem> maybeScheduleItem = sortedScheduleItems
                    .FirstOrDefault(item => CollectionKey.ForScheduleItem(item) == collectionKey);
                PlaybackOrder playbackOrder = maybeScheduleItem
                    .Match(item => item.PlaybackOrder, () => PlaybackOrder.Shuffle);
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
            
            // removing any items scheduled past the start anchor
            // this could happen if the app was closed after scheduling items
            // but before saving the anchor
            int removed = playout.Items.RemoveAll(pi => pi.StartOffset >= currentTime);
            if (removed > 0)
            {
                _logger.LogWarning("Removed {Count} schedule items beyond current start anchor", removed);
            }

            // start with the previously-decided schedule item
            // start with the previous multiple/duration states
            var playoutBuilderState = new PlayoutBuilderState(
                sortedScheduleItems.IndexOf(startAnchor.NextScheduleItem),
                Optional(startAnchor.MultipleRemaining),
                startAnchor.DurationFinishOffset,
                startAnchor.InFlood,
                startAnchor.InDurationFiller,
                Optional(startAnchor.MultipleRemaining).IsSome || startAnchor.DurationFinishOffset.IsSome,
                currentTime);

            var schedulerOne = new PlayoutModeSchedulerOne();
            var schedulerMultiple = new PlayoutModeSchedulerMultiple(collectionMediaItems);
            var schedulerDuration = new PlayoutModeSchedulerDuration();
            var schedulerFlood = new PlayoutModeSchedulerFlood(sortedScheduleItems);

            // loop until we're done filling the desired amount of time
            while (playoutBuilderState.CurrentTime < playoutFinish)
            {
                // get the schedule item out of the sorted list
                ProgramScheduleItem scheduleItem =
                    sortedScheduleItems[playoutBuilderState.ScheduleItemIndex % sortedScheduleItems.Count];

                ProgramScheduleItem nextScheduleItem =
                    sortedScheduleItems[(playoutBuilderState.ScheduleItemIndex + 1) % sortedScheduleItems.Count];

                Tuple<PlayoutBuilderState, List<PlayoutItem>> result = scheduleItem switch
                {
                    ProgramScheduleItemMultiple multiple => schedulerMultiple.Schedule(
                        playoutBuilderState,
                        collectionEnumerators,
                        multiple,
                        nextScheduleItem,
                        playoutFinish,
                        _logger),
                    ProgramScheduleItemDuration duration => schedulerDuration.Schedule(
                        playoutBuilderState,
                        collectionEnumerators,
                        duration,
                        nextScheduleItem,
                        playoutFinish,
                        _logger),
                    ProgramScheduleItemFlood flood => schedulerFlood.Schedule(
                        playoutBuilderState,
                        collectionEnumerators,
                        flood,
                        nextScheduleItem,
                        playoutFinish,
                        _logger),
                    ProgramScheduleItemOne one => schedulerOne.Schedule(
                        playoutBuilderState,
                        collectionEnumerators,
                        one,
                        nextScheduleItem,
                        playoutFinish,
                        _logger),
                    _ => throw new ArgumentOutOfRangeException(nameof(scheduleItem))
                };

                (PlayoutBuilderState nextState, List<PlayoutItem> playoutItems) = result;

                foreach (PlayoutItem playoutItem in playoutItems)
                {
                    playout.Items.Add(playoutItem);
                }

                playoutBuilderState = nextState;
            }

            // once more to get playout anchor
            ProgramScheduleItem anchorScheduleItem =
                sortedScheduleItems[playoutBuilderState.ScheduleItemIndex % sortedScheduleItems.Count];

            // build program schedule anchors
            playout.ProgramScheduleAnchors = BuildProgramScheduleAnchors(playout, collectionEnumerators);

            // remove any items outside the desired range
            playout.Items.RemoveAll(
                old => old.FinishOffset < playoutStart.AddHours(-4) || old.StartOffset > playoutFinish);

            if (playout.Items.Any())
            {
                DateTimeOffset maxStartTime = playout.Items.Max(i => i.FinishOffset);
                if (maxStartTime < playoutBuilderState.CurrentTime)
                {
                    playoutBuilderState = playoutBuilderState with { CurrentTime = maxStartTime };
                }
            }

            playout.Anchor = new PlayoutAnchor
            {
                NextScheduleItem = anchorScheduleItem,
                NextScheduleItemId = anchorScheduleItem.Id,
                NextStart = PlayoutModeSchedulerBase<ProgramScheduleItem>.GetStartTimeAfter(playoutBuilderState, anchorScheduleItem)
                    .UtcDateTime,
                MultipleRemaining = playoutBuilderState.MultipleRemaining.IsSome
                    ? playoutBuilderState.MultipleRemaining.ValueUnsafe()
                    : null,
                DurationFinish = playoutBuilderState.DurationFinish.IsSome
                    ? playoutBuilderState.DurationFinish.ValueUnsafe().UtcDateTime
                    : null,
                InFlood = playoutBuilderState.InFlood,
                InDurationFiller = playoutBuilderState.InDurationFiller
            };

            return playout;
        }

        private async Task<Map<CollectionKey, List<MediaItem>>> GetCollectionMediaItems(Playout playout)
        {
            var collectionKeys = playout.ProgramSchedule.Items
                .SelectMany(CollectionKeysForItem)
                .Distinct()
                .ToList();

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

            return Map.createRange(tuples);
        }

        private async Task<Option<CollectionKey>> CheckForEmptyCollections(
            Map<CollectionKey, List<MediaItem>> collectionMediaItems)
        {
            foreach ((CollectionKey _, List<MediaItem> items) in collectionMediaItems)
            {
                var zeroItems = new List<MediaItem>();

                foreach (MediaItem item in items)
                {
                    bool isZero = item switch
                    {
                        Movie m => await m.MediaVersions.Map(v => v.Duration).HeadOrNone()
                            .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                        Episode e => await e.MediaVersions.Map(v => v.Duration).HeadOrNone()
                            .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                        MusicVideo mv => await mv.MediaVersions.Map(v => v.Duration).HeadOrNone()
                            .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
                        OtherVideo ov => await ov.MediaVersions.Map(v => v.Duration).HeadOrNone()
                            .IfNoneAsync(TimeSpan.Zero) == TimeSpan.Zero,
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

            return collectionMediaItems.Find(c => !c.Value.Any()).Map(c => c.Key);
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
            else
            {
                result = await _mediaCollectionRepository.GetFakeMultiCollectionCollections(
                    collectionKey.CollectionId,
                    collectionKey.SmartCollectionId);
            }

            return result;
        }

        internal static string DisplayTitle(MediaItem mediaItem)
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
                case OtherVideo ov:
                    return ov.OtherVideoMetadata.HeadOrNone().Match(
                        ovm => ovm.Title ?? string.Empty,
                        () => "[unknown video]");
                default:
                    return string.Empty;
            }
        }

        private static List<CollectionKey> CollectionKeysForItem(ProgramScheduleItem item)
        {
            var result = new List<CollectionKey>
            {
                CollectionKey.ForScheduleItem(item)
            };

            if (item.PreRollFiller != null)
            {
                result.Add(CollectionKey.ForFillerPreset(item.PreRollFiller));
            }

            if (item.MidRollFiller != null)
            {
                result.Add(CollectionKey.ForFillerPreset(item.MidRollFiller));
            }

            if (item.PostRollFiller != null)
            {
                result.Add(CollectionKey.ForFillerPreset(item.PostRollFiller));
            }

            if (item.TailFiller != null)
            {
                result.Add(CollectionKey.ForFillerPreset(item.TailFiller));
            }

            if (item.FallbackFiller != null)
            {
                result.Add(CollectionKey.ForFillerPreset(item.FallbackFiller));
            }

            return result;
        }
    }
}
