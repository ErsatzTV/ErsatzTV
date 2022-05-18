using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class PlayoutModeSchedulerMultiple : PlayoutModeSchedulerBase<ProgramScheduleItemMultiple>
{
    private readonly Map<CollectionKey, List<MediaItem>> _collectionMediaItems;

    public PlayoutModeSchedulerMultiple(Map<CollectionKey, List<MediaItem>> collectionMediaItems, ILogger logger)
        : base(logger) =>
        _collectionMediaItems = collectionMediaItems;

    public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItemMultiple scheduleItem,
        ProgramScheduleItem nextScheduleItem,
        DateTimeOffset hardStop)
    {
        var playoutItems = new List<PlayoutItem>();

        DateTimeOffset firstStart = GetStartTimeAfter(playoutBuilderState, scheduleItem);
        if (firstStart >= hardStop)
        {
            playoutBuilderState = playoutBuilderState with { CurrentTime = hardStop };
            return Tuple(playoutBuilderState, playoutItems);
        }

        PlayoutBuilderState nextState = playoutBuilderState with
        {
            MultipleRemaining = playoutBuilderState.MultipleRemaining.IfNone(scheduleItem.Count)
        };

        if (nextState.MultipleRemaining == 0)
        {
            nextState = nextState with
            {
                MultipleRemaining = _collectionMediaItems[CollectionKey.ForScheduleItem(scheduleItem)].Count
            };
        }

        IMediaCollectionEnumerator contentEnumerator =
            collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
        while (contentEnumerator.Current.IsSome && nextState.MultipleRemaining > 0 &&
               nextState.CurrentTime < hardStop)
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
                CustomTitle = scheduleItem.CustomTitle,
                WatermarkId = scheduleItem.WatermarkId,
                PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                SubtitleMode = scheduleItem.SubtitleMode
            };

            // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);
            DateTimeOffset itemEndTimeWithFiller = CalculateEndTimeWithFiller(
                collectionEnumerators,
                scheduleItem,
                itemStartTime,
                itemDuration,
                itemChapters);

            playoutItems.AddRange(
                AddFiller(nextState, collectionEnumerators, scheduleItem, playoutItem, itemChapters));

            nextState = nextState with
            {
                CurrentTime = itemEndTimeWithFiller,
                MultipleRemaining = nextState.MultipleRemaining.Map(i => i - 1),

                // only bump guide group if we don't have a custom title
                NextGuideGroup = string.IsNullOrWhiteSpace(scheduleItem.CustomTitle)
                    ? nextState.IncrementGuideGroup
                    : nextState.NextGuideGroup
            };

            contentEnumerator.MoveNext();
        }

        if (nextState.MultipleRemaining.IfNone(-1) == 0)
        {
            // _logger.LogDebug(
            //     "Advancing to next schedule item after playout mode {PlayoutMode}",
            //     "Multiple");

            nextState = nextState with
            {
                MultipleRemaining = None,

                // only decrement guide group if it was bumped
                NextGuideGroup = playoutItems.Select(pi => pi.GuideGroup).Distinct().Count() != 1
                    ? nextState.DecrementGuideGroup
                    : nextState.NextGuideGroup
            };

            nextState.ScheduleItemsEnumerator.MoveNext();
        }

        DateTimeOffset nextItemStart = GetStartTimeAfter(nextState, nextScheduleItem);

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

        nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

        return Tuple(nextState, playoutItems);
    }
}
