﻿using System;
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

                MediaVersion version = mediaItem switch
                {
                    Movie m => m.MediaVersions.Head(),
                    Episode e => e.MediaVersions.Head(),
                    MusicVideo mv => mv.MediaVersions.Head(),
                    OtherVideo mv => mv.MediaVersions.Head(),
                    _ => throw new ArgumentOutOfRangeException(nameof(mediaItem))
                };

                if (version.Duration > scheduleItem.PlayoutDuration)
                {
                    _logger.LogWarning(
                        "Skipping playout item {Title} with duration {Duration} that is longer than schedule item duration {PlayoutDuration}",
                        PlayoutBuilder.DisplayTitle(mediaItem),
                        version.Duration,
                        scheduleItem.PlayoutDuration);

                    contentEnumerator.MoveNext();
                    continue;
                }

                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = itemStartTime.UtcDateTime,
                    Finish = itemStartTime.UtcDateTime + version.Duration,
                    CustomGroup = true,
                    FillerKind = scheduleItem.GuideMode == GuideMode.Filler
                        ? FillerKind.Tail
                        : FillerKind.None
                };
                
                DateTimeOffset durationFinish = nextState.DurationFinish.IfNone(SystemTime.MaxValueUtc);
                willFinishInTime = itemStartTime > durationFinish ||
                                   itemStartTime + version.Duration <= durationFinish;

                if (willFinishInTime)
                {
                    LogScheduledItem(scheduleItem, mediaItem, itemStartTime);
                    playoutItems.Add(playoutItem);

                    nextState = nextState with
                    {
                        CurrentTime = itemStartTime + version.Duration
                    };

                    contentEnumerator.MoveNext();
                }
                else
                {
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
                        Tuple<PlayoutBuilderState, List<PlayoutItem>> withTail = AddTailFiller(
                            nextState,
                            collectionEnumerators,
                            scheduleItem,
                            playoutItems,
                            nextItemStart);
                        if (scheduleItem.FallbackFiller != null)
                        {
                            return AddFallbackFiller(
                                withTail.Item1,
                                collectionEnumerators,
                                scheduleItem,
                                withTail.Item2,
                                nextItemStart);
                        }
                        else
                        {
                            PlayoutBuilderState finalState = withTail.Item1 with { CurrentTime = nextItemStart };
                            return Tuple(finalState, withTail.Item2);
                        }
                    case TailMode.Offline:
                        if (scheduleItem.FallbackFiller != null)
                        {
                            return AddFallbackFiller(
                                nextState,
                                collectionEnumerators,
                                scheduleItem,
                                playoutItems,
                                nextItemStart);
                        }
                        else
                        {
                            nextState = nextState with { CurrentTime = nextItemStart };
                        }

                        break;
                }
            }
            
            return Tuple(nextState, playoutItems);
        }
    }
}
