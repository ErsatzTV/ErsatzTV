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

            Option<IMediaCollectionEnumerator> maybePostRollEnumerator =
                Optional(scheduleItem.PostRollFiller)
                    .Map(fp => collectionEnumerators[CollectionKey.ForFillerPreset(fp)]);
            
            while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
            {
                MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();
                Option<MediaItem> maybePostRollItem = maybePostRollEnumerator
                    .Map(e => e.Current)
                    .MapT(identity)
                    .Flatten();

                // find when we should start this item, based on the current time
                DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);
                
                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = itemStartTime.UtcDateTime,
                    Finish = itemStartTime.UtcDateTime + itemDuration,
                    CustomGroup = true,
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

                TimeSpan postRollDuration = maybePostRollItem.Match(DurationForMediaItem, () => TimeSpan.Zero);

                DateTimeOffset itemEndTime = itemStartTime + itemDuration + postRollDuration;
                
                // if the current time is before the next schedule item, but the current finish
                // is after, we need to move on to the next schedule item
                // eventually, spots probably have to fit in this gap
                willFinishInTime = itemStartTime > peekScheduleItemStart ||
                                   itemEndTime <= peekScheduleItemStart;

                if (willFinishInTime)
                {
                    LogScheduledItem(scheduleItem, mediaItem, itemStartTime);
                    playoutItems.Add(playoutItem);

                    foreach (MediaItem postRollItem in maybePostRollItem)
                    {
                        var postRollPlayoutItem = new PlayoutItem
                        {
                            MediaItemId = postRollItem.Id,
                            Start = playoutItem.Finish,
                            Finish = playoutItem.Finish + postRollDuration,
                            CustomGroup = true,
                            FillerKind = FillerKind.PostRoll
                        };

                        playoutItems.Add(postRollPlayoutItem);

                        foreach (IMediaCollectionEnumerator enumerator in maybePostRollEnumerator)
                        {
                            enumerator.MoveNext();
                        }
                    }

                    nextState = nextState with
                    {
                        CurrentTime = itemEndTime,
                        InFlood = true
                    };

                    contentEnumerator.MoveNext();
                }
            }

            _logger.LogDebug(
                "Advancing to next schedule item after playout mode {PlayoutMode}",
                "Flood");

            nextState = nextState with
            {
                ScheduleItemIndex = nextState.ScheduleItemIndex + 1,
                InFlood = nextState.CurrentTime >= hardStop,
                CustomGroup = false
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

            return Tuple(nextState, playoutItems);
        }
    }
}
