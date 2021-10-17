using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public abstract class SchedulerBase : IScheduler
    {
        public async Task<Tuple<PlayoutBuilderState, Option<PlayoutItem>>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            List<ProgramScheduleItem> sortedScheduleItems,
            ProgramScheduleItem scheduleItem,
            ILogger logger)
        {
            // find when we should start this item, based on the current time
            DateTimeOffset itemStartTime = GetStartTimeAfter(
                playoutBuilderState,
                scheduleItem,
                playoutBuilderState.CurrentTime);

            IMediaCollectionEnumerator enumerator = GetEnumerator(
                playoutBuilderState,
                collectionEnumerators,
                scheduleItem);
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                MediaVersion version = mediaItem switch
                {
                    Movie m => m.MediaVersions.Head(),
                    Episode e => e.MediaVersions.Head(),
                    MusicVideo mv => mv.MediaVersions.Head(),
                    OtherVideo mv => mv.MediaVersions.Head(),
                    _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                };

                (PlayoutBuilderState newState, PlayoutItem playoutItem) =
                    ScheduleImpl(playoutBuilderState, collectionMediaItems, scheduleItem, mediaItem, version, itemStartTime, logger); 
                if (!string.IsNullOrWhiteSpace(scheduleItem.CustomTitle))
                {
                    playoutItem.CustomTitle = scheduleItem.CustomTitle;
                }
                
                enumerator.MoveNext();

                foreach (MediaItem peekMediaItem in enumerator.Current)
                {
                    newState = PeekState(
                        newState,
                        peekMediaItem,
                        collectionEnumerators,
                        sortedScheduleItems,
                        scheduleItem,
                        playoutItem,
                        itemStartTime,
                        logger);
                }

                return Tuple(newState, Some(playoutItem));
            }

            return Tuple(playoutBuilderState, Option<PlayoutItem>.None);
        }

        protected virtual IMediaCollectionEnumerator GetEnumerator(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItem scheduleItem) =>
            collectionEnumerators[CollectionKeyForItem(scheduleItem)];

        protected virtual PlayoutBuilderState PeekState(
            PlayoutBuilderState playoutBuilderState,
            MediaItem peekMediaItem,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            List<ProgramScheduleItem> sortedScheduleItems,
            ProgramScheduleItem scheduleItem,
            PlayoutItem playoutItem,
            DateTimeOffset itemStartTime,
            ILogger logger) =>
            playoutBuilderState;

        protected abstract Tuple<PlayoutBuilderState, PlayoutItem> ScheduleImpl(
            PlayoutBuilderState playoutBuilderState,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            ProgramScheduleItem scheduleItem,
            MediaItem mediaItem,
            MediaVersion version,
            DateTimeOffset itemStartTime,
            ILogger logger);

        public static DateTimeOffset GetStartTimeAfter(
            PlayoutBuilderState state,
            ProgramScheduleItem item,
            DateTimeOffset start)
        {
            switch (item.StartType)
            {
                case StartType.Fixed:
                    if (item is ProgramScheduleItemMultiple && state.MultipleRemaining.IsSome ||
                        item is ProgramScheduleItemDuration && state.DurationFinish.IsSome ||
                        item is ProgramScheduleItemFlood && state.InFlood ||
                        item is ProgramScheduleItemDuration && state.InDurationFiller)
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

        internal static CollectionKey CollectionKeyForItem(ProgramScheduleItem item) =>
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

        internal static Option<CollectionKey> TailCollectionKeyForItem(ProgramScheduleItem item)
        {
            if (item is ProgramScheduleItemDuration { TailMode: TailMode.Filler } duration)
            {
                return duration.TailCollectionType switch
                {
                    ProgramScheduleItemCollectionType.Collection => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        CollectionId = duration.TailCollectionId
                    },
                    ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        MediaItemId = duration.TailMediaItemId
                    },
                    ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        MediaItemId = duration.TailMediaItemId
                    },
                    ProgramScheduleItemCollectionType.Artist => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        MediaItemId = duration.TailMediaItemId
                    },
                    ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        MultiCollectionId = duration.TailMultiCollectionId
                    },
                    ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
                    {
                        CollectionType = duration.TailCollectionType,
                        SmartCollectionId = duration.TailSmartCollectionId
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(item))
                };
            }

            return None;
        }
    }
}
