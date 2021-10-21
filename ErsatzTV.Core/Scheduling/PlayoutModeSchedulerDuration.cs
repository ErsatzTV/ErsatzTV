using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerDuration : PlayoutModeSchedulerBase<ProgramScheduleItemDuration>
    {
        public PlayoutModeSchedulerDuration(ILogger logger) : base(logger)
        {
        }

        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemDuration scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop)
        {
            var playoutItems = new List<PlayoutItem>();

            PlayoutBuilderState nextState = playoutBuilderState;

            var willFinishInTime = true;
            Option<DateTimeOffset> durationUntil = None;

            IMediaCollectionEnumerator contentEnumerator =
                collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
            while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
            {
                MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);

                // remember when we need to finish this duration item
                if (nextState.DurationFinish.IsNone)
                {
                    nextState = nextState with
                    {
                        DurationFinish = itemStartTime + scheduleItem.PlayoutDuration
                    };

                    durationUntil = nextState.DurationFinish;
                }

                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                List<MediaChapter> itemChapters = ChaptersForMediaItem(mediaItem);

                if (itemDuration > scheduleItem.PlayoutDuration)
                {
                    _logger.LogWarning(
                        "Skipping playout item {Title} with duration {Duration} that is longer than schedule item duration {PlayoutDuration}",
                        PlayoutBuilder.DisplayTitle(mediaItem),
                        itemDuration,
                        scheduleItem.PlayoutDuration);

                    contentEnumerator.MoveNext();
                    continue;
                }

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
                
                durationUntil.Do(du => playoutItem.GuideFinish = du.UtcDateTime); 
                
                DateTimeOffset durationFinish = nextState.DurationFinish.IfNone(SystemTime.MaxValueUtc);
                DateTimeOffset itemEndTimeWithFiller = CalculateEndTimeWithFiller(
                    collectionEnumerators,
                    scheduleItem,
                    itemStartTime,
                    itemDuration,
                    itemChapters);
                willFinishInTime = itemStartTime > durationFinish ||
                                   itemEndTimeWithFiller <= durationFinish;
                if (willFinishInTime)
                {
                    // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);
                    playoutItems.AddRange(
                        AddFiller(nextState, collectionEnumerators, scheduleItem, playoutItem, itemChapters));

                    nextState = nextState with
                    {
                        CurrentTime = itemEndTimeWithFiller
                    };

                    contentEnumerator.MoveNext();
                }
                else
                {
                    TimeSpan durationBlock = itemEndTimeWithFiller - itemStartTime;
                    if (itemEndTimeWithFiller - itemStartTime > scheduleItem.PlayoutDuration)
                    {
                        _logger.LogWarning(
                            "Unable to schedule duration block of {DurationBlock} which is longer than the configured playout duration {PlayoutDuration}",
                            durationBlock,
                            scheduleItem.PlayoutDuration);
                    }

                    nextState = nextState with
                    {
                        DurationFinish = None,
                        ScheduleItemIndex = nextState.ScheduleItemIndex + 1
                    };
                }
            }

            // this is needed when the duration finish exactly matches the hard stop
            if (nextState.DurationFinish.IsSome && nextState.CurrentTime == nextState.DurationFinish)
            {
                nextState = nextState with
                {
                    DurationFinish = None,
                    ScheduleItemIndex = nextState.ScheduleItemIndex + 1
                };
            }

            foreach (DateTimeOffset nextItemStart in durationUntil)
            {
                switch (scheduleItem.TailMode)
                {
                    case TailMode.Filler:
                        if (scheduleItem.TailFiller != null)
                        {
                            (nextState, playoutItems) = AddTailFiller(
                                nextState,
                                collectionEnumerators,
                                scheduleItem,
                                playoutItems,
                                nextItemStart);
                        }

                        if (scheduleItem.FallbackFiller != null)
                        {
                            (nextState, playoutItems) = AddFallbackFiller(
                                nextState,
                                collectionEnumerators,
                                scheduleItem,
                                playoutItems,
                                nextItemStart);
                        }

                        nextState = nextState with { CurrentTime = nextItemStart };
                        break;
                    case TailMode.Offline:
                        if (scheduleItem.FallbackFiller != null)
                        {
                            (nextState, playoutItems) = AddFallbackFiller(
                                nextState,
                                collectionEnumerators,
                                scheduleItem,
                                playoutItems,
                                nextItemStart);
                        }

                        nextState = nextState with { CurrentTime = nextItemStart };
                        break;
                }
            }

            nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };
            
            return Tuple(nextState, playoutItems);
        }
    }
}
