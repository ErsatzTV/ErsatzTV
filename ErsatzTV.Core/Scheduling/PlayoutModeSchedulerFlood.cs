﻿using System;
using System.Collections.Generic;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerFlood : PlayoutModeSchedulerBase<ProgramScheduleItemFlood>
    {
        private readonly List<ProgramScheduleItem> _sortedScheduleItems;

        public PlayoutModeSchedulerFlood(List<ProgramScheduleItem> sortedScheduleItems, ILogger logger)
            : base(logger)
        {
            _sortedScheduleItems = sortedScheduleItems;
        }

        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemFlood scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop)
        {
            var playoutItems = new List<PlayoutItem>();

            PlayoutBuilderState nextState = playoutBuilderState;
            var willFinishInTime = true;

            IMediaCollectionEnumerator contentEnumerator =
                collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];

            while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
            {
                MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                List<MediaChapter> itemChapters = ChaptersForMediaItem(mediaItem);
                
                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = itemStartTime.UtcDateTime,
                    Finish = itemStartTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    GuideGroup = nextState.NextGuideGroup,
                    FillerKind = scheduleItem.GuideMode == GuideMode.Filler
                        ? FillerKind.Tail
                        : FillerKind.None
                };
                
                ProgramScheduleItem peekScheduleItem =
                    _sortedScheduleItems[(nextState.ScheduleItemIndex + 1) % _sortedScheduleItems.Count];
                DateTimeOffset peekScheduleItemStart =
                    peekScheduleItem.StartType == StartType.Fixed
                        ? GetStartTimeAfter(nextState, peekScheduleItem)
                        : DateTimeOffset.MaxValue;

                DateTimeOffset itemEndTimeWithFiller = CalculateEndTimeWithFiller(
                    collectionEnumerators,
                    scheduleItem,
                    itemStartTime,
                    itemDuration,
                    itemChapters);
                
                // if the current time is before the next schedule item, but the current finish
                // is after, we need to move on to the next schedule item
                willFinishInTime = itemStartTime > peekScheduleItemStart ||
                                   itemEndTimeWithFiller <= peekScheduleItemStart;

                if (willFinishInTime)
                {
                    playoutItems.AddRange(
                        AddFiller(nextState, collectionEnumerators, scheduleItem, playoutItem, itemChapters));
                    // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);

                    DateTimeOffset actualEndTime = playoutItems.Max(p => p.FinishOffset);
                    if (Math.Abs((itemEndTimeWithFiller - actualEndTime).TotalSeconds) > 1)
                    {
                        _logger.LogWarning(
                            "Filler prediction failure: predicted {PredictedDuration} doesn't match actual {ActualDuration}",
                            itemEndTimeWithFiller,
                            actualEndTime);

                        // _logger.LogWarning("Playout items: {@PlayoutItems}", playoutItems);
                    }

                    nextState = nextState with
                    {
                        CurrentTime = itemEndTimeWithFiller,
                        InFlood = true,
                        NextGuideGroup = nextState.IncrementGuideGroup
                    };

                    contentEnumerator.MoveNext();
                }
            }

            // _logger.LogDebug(
            //     "Advancing to next schedule item after playout mode {PlayoutMode}",
            //     "Flood");

            nextState = nextState with
            {
                ScheduleItemIndex = nextState.ScheduleItemIndex + 1,
                InFlood = nextState.CurrentTime >= hardStop,
                NextGuideGroup = nextState.DecrementGuideGroup
            };
            
            ProgramScheduleItem peekItem =
                _sortedScheduleItems[nextState.ScheduleItemIndex % _sortedScheduleItems.Count];
            DateTimeOffset peekItemStart = GetStartTimeAfter(nextState, peekItem);

            if (scheduleItem.TailFiller != null)
            {
                (nextState, playoutItems) = AddTailFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    peekItemStart);
            }

            if (scheduleItem.FallbackFiller != null)
            {
                (nextState, playoutItems) = AddFallbackFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItems,
                    peekItemStart);
            }

            nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

            return Tuple(nextState, playoutItems);
        }
    }
}
