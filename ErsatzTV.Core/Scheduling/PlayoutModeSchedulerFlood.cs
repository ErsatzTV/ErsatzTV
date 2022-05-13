using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class PlayoutModeSchedulerFlood : PlayoutModeSchedulerBase<ProgramScheduleItemFlood>
{
    public PlayoutModeSchedulerFlood(ILogger logger)
        : base(logger)
    {
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

        ProgramScheduleItem peekScheduleItem = nextScheduleItem;

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
                    : FillerKind.None,
                WatermarkId = scheduleItem.WatermarkId,
                PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                SubtitleMode = scheduleItem.SubtitleMode
            };

            DateTimeOffset peekScheduleItemStart =
                peekScheduleItem.StartType == StartType.Fixed
                    ? GetStartTimeAfter(nextState with { InFlood = false }, peekScheduleItem)
                    : DateTimeOffset.MaxValue;

            DateTimeOffset itemEndTimeWithFiller = CalculateEndTimeWithFiller(
                collectionEnumerators,
                scheduleItem,
                itemStartTime,
                itemDuration,
                itemChapters);

            // if the next schedule item is supposed to start during this item,
            // don't schedule this item and just move on
            willFinishInTime = peekScheduleItemStart < itemStartTime ||
                               peekScheduleItemStart >= itemEndTimeWithFiller;

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
            InFlood = nextState.CurrentTime >= hardStop,
            NextGuideGroup = nextState.DecrementGuideGroup
        };

        // only advance to the next schedule item if we aren't still in a flood
        if (!nextState.InFlood)
        {
            nextState.ScheduleItemsEnumerator.MoveNext();
        }

        ProgramScheduleItem peekItem = nextScheduleItem;
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
