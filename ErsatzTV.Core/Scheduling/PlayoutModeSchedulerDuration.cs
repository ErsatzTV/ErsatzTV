using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

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
                    : FillerKind.None,
                CustomTitle = scheduleItem.CustomTitle,
                WatermarkId = scheduleItem.WatermarkId
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
                    CurrentTime = itemEndTimeWithFiller,

                    // only bump guide group if we don't have a custom title
                    NextGuideGroup = string.IsNullOrWhiteSpace(scheduleItem.CustomTitle)
                        ? nextState.IncrementGuideGroup
                        : nextState.NextGuideGroup
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
                    DurationFinish = None
                };

                nextState.ScheduleItemsEnumerator.MoveNext();
            }
        }

        // this is needed when the duration finish exactly matches the hard stop
        if (nextState.DurationFinish.IsSome && nextState.CurrentTime == nextState.DurationFinish)
        {
            nextState = nextState with
            {
                DurationFinish = None
            };

            nextState.ScheduleItemsEnumerator.MoveNext();
        }

        if (playoutItems.Select(pi => pi.GuideGroup).Distinct().Count() != 1)
        {
            nextState = nextState with { NextGuideGroup = nextState.DecrementGuideGroup };
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

        // clear guide finish on all but the last item
        var all = playoutItems.Filter(pi => pi.FillerKind == FillerKind.None).ToList();
        PlayoutItem last = all.OrderBy(pi => pi.FinishOffset).LastOrDefault();
        foreach (PlayoutItem item in all.Filter(pi => pi != last))
        {
            item.GuideFinish = null;
        }

        nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

        return Tuple(nextState, playoutItems);
    }
}
