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
        DateTimeOffset hardStop,
        CancellationToken cancellationToken)
    {
        var playoutItems = new List<PlayoutItem>();

        PlayoutBuilderState nextState = playoutBuilderState;
        var willFinishInTime = true;

        IMediaCollectionEnumerator contentEnumerator =
            collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];

        ProgramScheduleItem peekScheduleItem = nextScheduleItem;

        var scheduledNone = false;

        while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
        {
            MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

            // find when we should start this item, based on the current time
            DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);
            if (itemStartTime >= hardStop)
            {
                scheduledNone = playoutItems.Count == 0;
                nextState = nextState with { CurrentTime = hardStop };
                break;
            }

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
                    ? FillerKind.GuideMode
                    : FillerKind.None,
                CustomTitle = scheduleItem.CustomTitle,
                WatermarkId = scheduleItem.WatermarkId,
                PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                SubtitleMode = scheduleItem.SubtitleMode
            };

            // never block scheduling when there is only one schedule item (with fixed start and flood) 
            DateTimeOffset peekScheduleItemStart =
                scheduleItem.Id != peekScheduleItem.Id && peekScheduleItem.StartType == StartType.Fixed
                    ? GetStartTimeAfter(nextState with { InFlood = false }, peekScheduleItem)
                    : DateTimeOffset.MaxValue;

            var enumeratorStates = new Dictionary<CollectionKey, CollectionEnumeratorState>();
            foreach ((CollectionKey key, IMediaCollectionEnumerator enumerator) in collectionEnumerators)
            {
                enumeratorStates.Add(key, enumerator.State.Clone());
            }

            List<PlayoutItem> maybePlayoutItems = AddFiller(
                nextState,
                collectionEnumerators,
                scheduleItem,
                playoutItem,
                itemChapters,
                false,
                cancellationToken);

            DateTimeOffset itemEndTimeWithFiller = maybePlayoutItems.Max(pi => pi.FinishOffset);

            // if the next schedule item is supposed to start during this item,
            // don't schedule this item and just move on
            willFinishInTime = peekScheduleItemStart < itemStartTime ||
                               peekScheduleItemStart >= itemEndTimeWithFiller;

            if (willFinishInTime)
            {
                playoutItems.AddRange(maybePlayoutItems);
                // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);

                nextState = nextState with
                {
                    CurrentTime = itemEndTimeWithFiller,
                    InFlood = true,

                    // only bump guide group if we don't have a custom title
                    NextGuideGroup = string.IsNullOrWhiteSpace(scheduleItem.CustomTitle)
                        ? nextState.IncrementGuideGroup
                        : nextState.NextGuideGroup
                };

                contentEnumerator.MoveNext();
            }
            else
            {
                // reset enumerators
                foreach ((CollectionKey key, IMediaCollectionEnumerator enumerator) in collectionEnumerators)
                {
                    enumerator.ResetState(enumeratorStates[key]);
                }
            }
        }

        // _logger.LogDebug(
        //     "Advancing to next schedule item after playout mode {PlayoutMode}",
        //     "Flood");

        nextState = nextState with
        {
            InFlood = playoutItems.Count != 0 && nextState.CurrentTime >= hardStop,

            // only decrement guide group if it was bumped
            NextGuideGroup = playoutItems.Select(pi => pi.GuideGroup).Distinct().Count() != 1
                ? nextState.DecrementGuideGroup
                : nextState.NextGuideGroup
        };

        // only advance to the next schedule item if we aren't still in a flood
        if (!nextState.InFlood && !scheduledNone)
        {
            nextState.ScheduleItemsEnumerator.MoveNext();
        }

        ProgramScheduleItem peekItem = nextScheduleItem;
        DateTimeOffset peekItemStart = GetFillerStartTimeAfter(nextState, peekItem, hardStop);

        if (scheduleItem.TailFiller != null)
        {
            (nextState, playoutItems) = AddTailFiller(
                nextState,
                collectionEnumerators,
                scheduleItem,
                playoutItems,
                peekItemStart,
                cancellationToken);
        }

        if (scheduleItem.FallbackFiller != null)
        {
            (nextState, playoutItems) = AddFallbackFiller(
                nextState,
                collectionEnumerators,
                scheduleItem,
                playoutItems,
                peekItemStart,
                cancellationToken);
        }

        nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

        return Tuple(nextState, playoutItems);
    }
}
