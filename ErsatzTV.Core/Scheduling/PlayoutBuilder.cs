using System;
using System.Collections.Generic;
using System.IO;
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
    public class PlayoutBuilder : IPlayoutBuilder
    {
        private static readonly Random Random = new();
        private readonly ILogger<PlayoutBuilder> _logger;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public PlayoutBuilder(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository,
            ILogger<PlayoutBuilder> logger)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
            _logger = logger;
        }

        public Task<Playout> BuildPlayoutItems(Playout playout, bool rebuild = false)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            return BuildPlayoutItems(playout, now, now.AddDays(2), rebuild);
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

            IEnumerable<Tuple<CollectionKey, List<MediaItem>>> tuples = await collectionKeys.Map(
                async collectionKey =>
                {
                    switch (collectionKey.CollectionType)
                    {
                        case ProgramScheduleItemCollectionType.Collection:
                            Option<List<MediaItem>> maybeItems =
                                await _mediaCollectionRepository.GetItems(collectionKey.Id);
                            return Tuple(collectionKey, maybeItems.IfNone(new List<MediaItem>()));
                        case ProgramScheduleItemCollectionType.TelevisionShow:
                            List<TelevisionEpisodeMediaItem> showItems =
                                await _televisionRepository.GetShowItems(collectionKey.Id);
                            return Tuple(collectionKey, showItems.Cast<MediaItem>().ToList());
                        case ProgramScheduleItemCollectionType.TelevisionSeason:
                            List<TelevisionEpisodeMediaItem> seasonItems =
                                await _televisionRepository.GetSeasonItems(collectionKey.Id);
                            return Tuple(collectionKey, seasonItems.Cast<MediaItem>().ToList());
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

            Option<CollectionKey> emptyCollection = collectionMediaItems.Find(c => !c.Value.Any()).Map(c => c.Key);
            if (emptyCollection.IsSome)
            {
                _logger.LogError(
                    "Unable to rebuild playout; collection {@CollectionKey} has no items!",
                    emptyCollection.ValueUnsafe());

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
            Map<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators =
                MapExtensions.Map(collectionMediaItems, (c, i) => GetMediaCollectionEnumerator(playout, c, i));

            // find start anchor
            PlayoutAnchor startAnchor = FindStartAnchor(playout, playoutStart, sortedScheduleItems);

            // start at the previously-decided time
            DateTimeOffset currentTime = startAnchor.NextStart;
            _logger.LogDebug(
                "Starting playout {PlayoutId} for channel {ChannelNumber} - {ChannelName} at {StartTime}",
                playout.Id,
                playout.Channel.Number,
                playout.Channel.Name,
                currentTime);

            // start with the previously-decided schedule item
            int index = sortedScheduleItems.IndexOf(startAnchor.NextScheduleItem);

            Option<int> multipleRemaining = None;
            Option<DateTimeOffset> durationFinish = None;
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
                    durationFinish.IsSome);

                IMediaCollectionEnumerator enumerator = collectionEnumerators[CollectionKeyForItem(scheduleItem)];
                enumerator.Current.IfSome(
                    mediaItem =>
                    {
                        _logger.LogDebug(
                            "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
                            scheduleItem.Index,
                            scheduleItem.CollectionType,
                            mediaItem.Id,
                            DisplayTitle(mediaItem),
                            itemStartTime);

                        var playoutItem = new PlayoutItem
                        {
                            MediaItemId = mediaItem.Id,
                            Start = itemStartTime.UtcDateTime,
                            Finish = itemStartTime.UtcDateTime + mediaItem.Statistics.Duration
                        };

                        currentTime = itemStartTime + mediaItem.Statistics.Duration;
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
                                break;
                            case ProgramScheduleItemMultiple multiple:
                                if (multipleRemaining.IsNone)
                                {
                                    multipleRemaining = multiple.Count;
                                }

                                multipleRemaining = multipleRemaining.Map(i => i - 1);
                                if (multipleRemaining.IfNone(-1) == 0)
                                {
                                    _logger.LogDebug(
                                        "Advancing to next schedule item after playout mode {PlayoutMode}",
                                        "Multiple");
                                    index++;
                                    multipleRemaining = None;
                                }

                                break;
                            case ProgramScheduleItemFlood:
                                enumerator.Current.Do(
                                    peekMediaItem =>
                                    {
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
                                                                   currentTime + peekMediaItem.Statistics.Duration >
                                                                   peekScheduleItemStart;
                                        if (willNotFinishInTime)
                                        {
                                            _logger.LogDebug(
                                                "Advancing to next schedule item after playout mode {PlayoutMode}",
                                                "Flood");
                                            index++;
                                        }
                                    });
                                break;
                            case ProgramScheduleItemDuration duration:
                                enumerator.Current.Do(
                                    peekMediaItem =>
                                    {
                                        // remember when we need to finish this duration item
                                        if (durationFinish.IsNone)
                                        {
                                            durationFinish = itemStartTime + duration.PlayoutDuration;
                                        }

                                        bool willNotFinishInTime =
                                            currentTime <= durationFinish.IfNone(DateTime.MinValue) &&
                                            currentTime + peekMediaItem.Statistics.Duration >
                                            durationFinish.IfNone(DateTime.MinValue);
                                        if (willNotFinishInTime)
                                        {
                                            _logger.LogDebug(
                                                "Advancing to next schedule item after playout mode {PlayoutMode}",
                                                "Duration");
                                            index++;

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
            playout.Anchor = new PlayoutAnchor
            {
                NextScheduleItem = nextScheduleItem,
                NextScheduleItemId = nextScheduleItem.Id,
                NextStart = GetStartTimeAfter(nextScheduleItem, currentTime).UtcDateTime
            };

            // build program schedule anchors
            playout.ProgramScheduleAnchors = BuildProgramScheduleAnchors(playout, collectionEnumerators);

            // remove any items outside the desired range
            playout.Items.RemoveAll(old => old.Finish < playoutStart || old.Start > playoutFinish);

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
                                NextStart = start.Date + schedule.StartTime.GetValueOrDefault()
                            };
                        case StartType.Dynamic:
                        default:
                            return new PlayoutAnchor
                            {
                                NextScheduleItem = schedule,
                                NextScheduleItemId = schedule.Id,
                                NextStart = start.Date
                            };
                    }
                });

        private static DateTimeOffset GetStartTimeAfter(
            ProgramScheduleItem item,
            DateTimeOffset start,
            bool inMultiple = false,
            bool inDuration = false)
        {
            switch (item.StartType)
            {
                case StartType.Fixed:
                    if (item is ProgramScheduleItemMultiple && inMultiple ||
                        item is ProgramScheduleItemDuration && inDuration)
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
            Map<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators)
        {
            var result = new List<PlayoutProgramScheduleAnchor>();

            foreach (CollectionKey collectionKey in collectionEnumerators.Keys)
            {
                Option<PlayoutProgramScheduleAnchor> maybeExisting = playout.ProgramScheduleAnchors
                    .FirstOrDefault(
                        a => a.CollectionType == collectionKey.CollectionType && a.CollectionId == collectionKey.Id);

                var maybeEnumeratorState = collectionEnumerators.GroupBy(e => e.Key, e => e.Value.State)
                    .ToDictionary(mcs => mcs.Key, mcs => mcs.Head());

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
                        CollectionId = collectionKey.Id,
                        EnumeratorState = maybeEnumeratorState[collectionKey]
                    });

                result.Add(scheduleAnchor);
            }

            return result;
        }

        private static IMediaCollectionEnumerator GetMediaCollectionEnumerator(
            Playout playout,
            CollectionKey collectionKey,
            List<MediaItem> mediaItems)
        {
            Option<PlayoutProgramScheduleAnchor> maybeAnchor = playout.ProgramScheduleAnchors
                .FirstOrDefault(
                    a => a.ProgramScheduleId == playout.ProgramScheduleId && a.CollectionType ==
                                                                          collectionKey.CollectionType
                                                                          && a.CollectionId == collectionKey.Id);

            MediaCollectionEnumeratorState state = maybeAnchor.Match(
                anchor => anchor.EnumeratorState,
                () => new MediaCollectionEnumeratorState { Seed = Random.Next(), Index = 0 });

            switch (playout.ProgramSchedule.MediaCollectionPlaybackOrder)
            {
                case PlaybackOrder.Chronological:
                    return new ChronologicalMediaCollectionEnumerator(mediaItems, state);
                case PlaybackOrder.Random:
                    return new RandomizedMediaCollectionEnumerator(mediaItems, state);
                case PlaybackOrder.Shuffle:
                    return new ShuffledMediaCollectionEnumerator(mediaItems, state);
                default:
                    // TODO: handle this error case differently?
                    return new RandomizedMediaCollectionEnumerator(mediaItems, state);
            }
        }

        private static string DisplayTitle(MediaItem mediaItem) =>
            mediaItem switch
            {
                TelevisionEpisodeMediaItem e => e.Metadata != null
                    ? $"{e.Metadata.Title} - s{e.Metadata.Season:00}e{e.Metadata.Episode:00}"
                    : Path.GetFileName(e.Path),
                Movie m => m.Metadata?.Title ?? Path.GetFileName(m.Path),
                _ => string.Empty
            };

        private static CollectionKey CollectionKeyForItem(ProgramScheduleItem item) =>
            item.CollectionType switch
            {
                ProgramScheduleItemCollectionType.Collection => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    Id = item.MediaCollectionId.Value
                },
                ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    Id = item.TelevisionShowId.Value
                },
                ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
                {
                    CollectionType = item.CollectionType,
                    Id = item.TelevisionSeasonId.Value
                },
                _ => throw new ArgumentOutOfRangeException(nameof(item))
            };

        private class CollectionKey : Record<CollectionKey>
        {
            public ProgramScheduleItemCollectionType CollectionType { get; set; }
            public int Id { get; set; }
        }
    }
}
