using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public abstract class PlayoutModeSchedulerBase<T> : IPlayoutModeScheduler<T> where T : ProgramScheduleItem
    {
        public static DateTimeOffset GetStartTimeAfter(
            PlayoutBuilderState state,
            ProgramScheduleItem scheduleItem)
        {
            DateTimeOffset startTime = state.CurrentTime;

            bool isIncomplete = scheduleItem is ProgramScheduleItemMultiple && state.MultipleRemaining.IsSome ||
                                scheduleItem is ProgramScheduleItemDuration && state.DurationFinish.IsSome ||
                                scheduleItem is ProgramScheduleItemFlood && state.InFlood ||
                                scheduleItem is ProgramScheduleItemDuration && state.InDurationFiller;
            
            if (scheduleItem.StartType == StartType.Fixed && !isIncomplete)
            {
                TimeSpan itemStartTime = scheduleItem.StartTime.GetValueOrDefault();
                DateTimeOffset result = startTime.Date + itemStartTime;
                // need to wrap to the next day if appropriate
                startTime = startTime.TimeOfDay > itemStartTime ? result.AddDays(1) : result;
            }

            return startTime;
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
        
        internal static CollectionKey FillerCollectionKey(FillerPreset filler) =>
            filler.CollectionType switch
            {
                ProgramScheduleItemCollectionType.Collection => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    CollectionId = filler.CollectionId
                },
                ProgramScheduleItemCollectionType.TelevisionShow => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    MediaItemId = filler.MediaItemId
                },
                ProgramScheduleItemCollectionType.TelevisionSeason => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    MediaItemId = filler.MediaItemId
                },
                ProgramScheduleItemCollectionType.Artist => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    MediaItemId = filler.MediaItemId
                },
                ProgramScheduleItemCollectionType.MultiCollection => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    MultiCollectionId = filler.MultiCollectionId
                },
                ProgramScheduleItemCollectionType.SmartCollection => new CollectionKey
                {
                    CollectionType = filler.CollectionType,
                    SmartCollectionId = filler.SmartCollectionId
                },
                _ => throw new ArgumentOutOfRangeException(nameof(filler))
            };

        public abstract Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            T scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger);
    }
}
