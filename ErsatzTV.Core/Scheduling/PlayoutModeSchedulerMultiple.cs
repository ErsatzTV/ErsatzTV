using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class PlayoutModeSchedulerMultiple : PlayoutModeSchedulerBase<ProgramScheduleItemMultiple>
{
    private readonly Map<CollectionKey, int> _collectionItemCount;

    public PlayoutModeSchedulerMultiple(Map<CollectionKey, int> collectionItemCount, ILogger logger)
        : base(logger) =>
        _collectionItemCount = collectionItemCount;

    public override Tuple<PlayoutBuilderState, List<PlayoutItem>> Schedule(
        PlayoutBuilderState playoutBuilderState,
        Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
        ProgramScheduleItemMultiple scheduleItem,
        ProgramScheduleItem nextScheduleItem,
        DateTimeOffset hardStop,
        CancellationToken cancellationToken)
    {
        var playoutItems = new List<PlayoutItem>();

        DateTimeOffset firstStart = GetStartTimeAfter(playoutBuilderState, scheduleItem, Option<ILogger>.Some(Logger));
        if (firstStart >= hardStop)
        {
            playoutBuilderState = playoutBuilderState with { CurrentTime = hardStop };
            return Tuple(playoutBuilderState, playoutItems);
        }

        PlayoutBuilderState nextState = playoutBuilderState with
        {
            CurrentTime = firstStart,
            MultipleRemaining = playoutBuilderState.MultipleRemaining.IfNone(scheduleItem.Count)
        };

        IMediaCollectionEnumerator contentEnumerator =
            collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];

        if (nextState.MultipleRemaining == 0)
        {
            switch (scheduleItem.MultipleMode)
            {
                case MultipleMode.CollectionSize:
                    nextState = nextState with
                    {
                        MultipleRemaining = _collectionItemCount[CollectionKey.ForScheduleItem(scheduleItem)]
                    };
                    break;
                case MultipleMode.PlaylistItemSize:
                    if (contentEnumerator is PlaylistEnumerator { CurrentEnumeratorPlayAll: true } playlistEnumerator)
                    {
                        nextState = nextState with
                        {
                            MultipleRemaining = playlistEnumerator
                                .ChildEnumerators[playlistEnumerator.EnumeratorIndex]
                                .Enumerator.Count
                        };
                    }

                    break;
                case MultipleMode.MultiEpisodeGroupSize:
                    if (contentEnumerator is ChronologicalMediaCollectionEnumerator chronologicalEnumerator)
                    {
                        foreach (MediaItem current in contentEnumerator.Current)
                        {
                            nextState = nextState with
                            {
                                MultipleRemaining = chronologicalEnumerator.GroupSizeForMediaItem(current)
                            };
                        }
                    }

                    break;
            }
        }

        while (contentEnumerator.Current.IsSome && nextState.MultipleRemaining > 0 &&
               nextState.CurrentTime < hardStop)
        {
            MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

            // find when we should start this item, based on the current time
            DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem, Option<ILogger>.Some(Logger));

            TimeSpan itemDuration = DurationForMediaItem(mediaItem);
            List<MediaChapter> itemChapters = ChaptersForMediaItem(mediaItem);

            var playoutItem = new PlayoutItem
            {
                PlayoutId = playoutBuilderState.PlayoutId,
                MediaItemId = mediaItem.Id,
                Start = itemStartTime.UtcDateTime,
                Finish = itemStartTime.UtcDateTime + itemDuration,
                InPoint = TimeSpan.Zero,
                OutPoint = itemDuration,
                GuideGroup = nextState.NextGuideGroup,
                FillerKind = scheduleItem.GuideMode == GuideMode.Filler ||
                             contentEnumerator.CurrentIncludeInProgramGuide == false
                    ? FillerKind.GuideMode
                    : FillerKind.None,
                CustomTitle = scheduleItem.CustomTitle,
                PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                SubtitleMode = scheduleItem.SubtitleMode,
                PlayoutItemWatermarks = []
            };

            foreach (ProgramScheduleItemWatermark programScheduleItemWatermark in scheduleItem
                         .ProgramScheduleItemWatermarks ?? [])
            {
                playoutItem.PlayoutItemWatermarks.Add(
                    new PlayoutItemWatermark
                    {
                        PlayoutItem = playoutItem,
                        WatermarkId = programScheduleItemWatermark.WatermarkId
                    });
            }

            // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);

            playoutItems.AddRange(
                AddFiller(
                    nextState,
                    collectionEnumerators,
                    scheduleItem,
                    playoutItem,
                    itemChapters,
                    true,
                    cancellationToken));

            nextState = nextState with
            {
                CurrentTime = playoutItems.Max(pi => pi.FinishOffset),
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

        DateTimeOffset nextItemStart = GetFillerStartTimeAfter(nextState, nextScheduleItem, hardStop);

        if (scheduleItem.TailFiller != null)
        {
            (nextState, playoutItems) = AddTailFiller(
                nextState,
                collectionEnumerators,
                scheduleItem,
                playoutItems,
                nextItemStart,
                cancellationToken);
        }

        if (scheduleItem.FallbackFiller != null)
        {
            (nextState, playoutItems) = AddFallbackFiller(
                nextState,
                collectionEnumerators,
                scheduleItem,
                playoutItems,
                nextItemStart,
                cancellationToken);
        }

        nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

        return Tuple(nextState, playoutItems);
    }
}
