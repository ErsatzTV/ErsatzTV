using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class PlayoutModeSchedulerDuration : PlayoutModeSchedulerBase<ProgramScheduleItemDuration>
    {
        public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItemDuration scheduleItem,
            ProgramScheduleItem nextScheduleItem,
            DateTimeOffset hardStop,
            ILogger logger)
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

                // remember when we need to finish this duration item
                if (nextState.DurationFinish.IsNone)
                {
                    nextState = nextState with
                    {
                        DurationFinish = itemStartTime + scheduleItem.PlayoutDuration
                    };
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
                    logger.LogWarning(
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
                    IsFiller = scheduleItem.GuideMode == GuideMode.Filler
                };
                
                DateTimeOffset durationFinish = nextState.DurationFinish.IfNone(SystemTime.MaxValueUtc);
                willFinishInTime = itemStartTime > durationFinish ||
                                   itemStartTime + version.Duration <= durationFinish;

                if (willFinishInTime)
                {
                    logger.LogDebug(
                        "Scheduling media item: {ScheduleItemNumber} / {CollectionType} / {MediaItemId} - {MediaItemTitle} / {StartTime}",
                        scheduleItem.Index,
                        scheduleItem.CollectionType,
                        mediaItem.Id,
                        PlayoutBuilder.DisplayTitle(mediaItem),
                        itemStartTime);

                    playoutItems.Add(playoutItem);

                    nextState = nextState with
                    {
                        CurrentTime = itemStartTime + version.Duration
                    };

                    contentEnumerator.MoveNext();
                }
                else
                {
                    logger.LogDebug("Done with duration");
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
                    // ScheduleItemIndex = nextState.ScheduleItemIndex + 1
                };
            }

            return Tuple(nextState, playoutItems);
        }
    }
}
