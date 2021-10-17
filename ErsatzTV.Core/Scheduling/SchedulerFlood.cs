using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class SchedulerFlood : SchedulerBase
    {
        protected override Tuple<PlayoutBuilderState, List<PlayoutItem>> ScheduleImpl(
            PlayoutBuilderState playoutBuilderState,
            Map<CollectionKey, List<MediaItem>> collectionMediaItems,
            ProgramScheduleItem scheduleItem,
            MediaItem mediaItem,
            MediaVersion version,
            DateTimeOffset itemStartTime,
            ILogger logger)
        {
            var playoutItem = new PlayoutItem
            {
                MediaItemId = mediaItem.Id,
                Start = itemStartTime.UtcDateTime,
                Finish = itemStartTime.UtcDateTime + version.Duration,
                CustomGroup = playoutBuilderState.CustomGroup,
                IsFiller = scheduleItem.GuideMode == GuideMode.Filler
            };

            PlayoutBuilderState nextState = playoutBuilderState with
            {
                CurrentTime = itemStartTime + version.Duration
            };

            return Tuple(nextState, new List<PlayoutItem> { playoutItem });
        }

        protected override PlayoutBuilderState PeekState(
            PlayoutBuilderState playoutBuilderState,
            MediaItem peekMediaItem,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            List<ProgramScheduleItem> sortedScheduleItems,
            ProgramScheduleItem scheduleItem,
            List<PlayoutItem> playoutItems,
            DateTimeOffset itemStartTime,
            ILogger logger)
        {
            PlayoutBuilderState nextState = playoutBuilderState with
            {
                CustomGroup = true
            };

            MediaVersion peekVersion = peekMediaItem switch
            {
                Movie m => m.MediaVersions.Head(),
                Episode e => e.MediaVersions.Head(),
                MusicVideo mv => mv.MediaVersions.Head(),
                OtherVideo ov => ov.MediaVersions.Head(),
                _ => throw new ArgumentOutOfRangeException(nameof(peekMediaItem))
            };

            ProgramScheduleItem peekScheduleItem =
                sortedScheduleItems[(nextState.ScheduleItemIndex + 1) % sortedScheduleItems.Count];
            DateTimeOffset peekScheduleItemStart =
                peekScheduleItem.StartType == StartType.Fixed
                    ? GetStartTimeAfter(nextState, peekScheduleItem, playoutBuilderState.CurrentTime)
                    : DateTimeOffset.MaxValue;

            // if the current time is before the next schedule item, but the current finish
            // is after, we need to move on to the next schedule item
            // eventually, spots probably have to fit in this gap
            bool willNotFinishInTime = playoutBuilderState.CurrentTime <= peekScheduleItemStart &&
                                       playoutBuilderState.CurrentTime + peekVersion.Duration >
                                       peekScheduleItemStart;
            if (willNotFinishInTime)
            {
                logger.LogDebug(
                    "Advancing to next schedule item after playout mode {PlayoutMode}",
                    "Flood");

                nextState = nextState with
                {
                    ScheduleItemIndex = nextState.ScheduleItemIndex + 1,
                    CustomGroup = false,
                    InFlood = false
                };
            }
            else
            {
                nextState = nextState with { InFlood = true };
            }

            return nextState;
        }
    }
}
