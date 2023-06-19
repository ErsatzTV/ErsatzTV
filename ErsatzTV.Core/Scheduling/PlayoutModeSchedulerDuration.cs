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
        DateTimeOffset hardStop,
        CancellationToken cancellationToken)
    {
        var playoutItems = new List<PlayoutItem>();

        PlayoutBuilderState nextState = playoutBuilderState;

        var willFinishInTime = true;
        Option<DateTimeOffset> durationUntil = None;
        int discardAttempts = 0;

        IMediaCollectionEnumerator contentEnumerator =
            collectionEnumerators[CollectionKey.ForScheduleItem(scheduleItem)];
        while (contentEnumerator.Current.IsSome && nextState.CurrentTime < hardStop && willFinishInTime)
        {
            MediaItem mediaItem = contentEnumerator.Current.ValueUnsafe();

            // find when we should start this item, based on the current time
            DateTimeOffset itemStartTime = GetStartTimeAfter(nextState, scheduleItem);

            if (itemStartTime >= hardStop)
            {
                nextState = nextState with { CurrentTime = hardStop };
                break;
            }

            // remember when we need to finish this duration item
            if (nextState.DurationFinish.IsNone)
            {
                nextState = nextState with
                {
                    DurationFinish = itemStartTime + scheduleItem.PlayoutDuration
                };
            }

            durationUntil = nextState.DurationFinish;

            TimeSpan itemDuration = DurationForMediaItem(mediaItem);
            List<MediaChapter> itemChapters = ChaptersForMediaItem(mediaItem);

            if (itemDuration > scheduleItem.PlayoutDuration)
            {
                _logger.LogWarning(
                    "Skipping playout item {Title} with duration {Duration:hh\\:mm\\:ss} that will never fit in schedule item duration {PlayoutDuration:hh\\:mm\\:ss}",
                    PlayoutBuilder.DisplayTitle(mediaItem),
                    itemDuration,
                    scheduleItem.PlayoutDuration);

                contentEnumerator.MoveNext();
                continue;
            }

            TimeSpan remainingDuration = durationUntil.ValueUnsafe() - itemStartTime;
            if (scheduleItem.DiscardToFillAttempts > 0 &&
                remainingDuration >= contentEnumerator.MinimumDuration.IfNone(TimeSpan.Zero) &&
                itemDuration > remainingDuration)
            {
                discardAttempts++;
                if (discardAttempts > scheduleItem.DiscardToFillAttempts)
                {
                    nextState = nextState with
                    {
                        DurationFinish = None
                    };

                    nextState.ScheduleItemsEnumerator.MoveNext();
                }
                else
                {
                    _logger.LogDebug(
                        "Skipping playout item {Title} with duration {Duration:hh\\:mm\\:ss} that is longer than remaining duration {RemainingDuration:hh\\:mm\\:ss}",
                        PlayoutBuilder.DisplayTitle(mediaItem),
                        itemDuration,
                        remainingDuration);

                    contentEnumerator.MoveNext();
                }
            }
            else
            {
                discardAttempts = 0;

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

                durationUntil.Do(du => playoutItem.GuideFinish = du.UtcDateTime);

                DateTimeOffset durationFinish = nextState.DurationFinish.IfNone(SystemTime.MaxValueUtc);
                
                var enumeratorClones = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();
                foreach ((CollectionKey key, IMediaCollectionEnumerator enumerator) in collectionEnumerators)
                {
                    IMediaCollectionEnumerator clone = enumerator.Clone(enumerator.State.Clone(), cancellationToken);
                    enumeratorClones.Add(key, clone);
                }

                List<PlayoutItem> maybePlayoutItems = AddFiller(
                    nextState,
                    enumeratorClones,
                    scheduleItem,
                    playoutItem,
                    itemChapters,
                    log: false,
                    cancellationToken);
                
                DateTimeOffset itemEndTimeWithFiller = maybePlayoutItems.Max(pi => pi.FinishOffset);

                willFinishInTime = itemStartTime > durationFinish ||
                                   itemEndTimeWithFiller <= durationFinish;
                if (willFinishInTime)
                {
                    // LogScheduledItem(scheduleItem, mediaItem, itemStartTime);
                    playoutItems.AddRange(maybePlayoutItems);

                    // update original enumerators
                    foreach ((CollectionKey key, IMediaCollectionEnumerator enumerator) in collectionEnumerators)
                    {
                        IMediaCollectionEnumerator clone = enumeratorClones[key];
                        while (enumerator.State.Seed != clone.State.Seed || enumerator.State.Index != clone.State.Index)
                        {
                            enumerator.MoveNext();
                        }
                    }

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
                            "Unable to schedule duration block of {DurationBlock:hh\\:mm\\:ss} which is longer than the configured playout duration {PlayoutDuration:hh\\:mm\\:ss}",
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
                            nextItemStart,
                            cancellationToken);
                    }

                    nextState = nextState with { CurrentTime = nextItemStart };
                    break;
            }
        }

        bool hasFallback = playoutItems.Any(p => p.FillerKind == FillerKind.Fallback);

        var playoutItemsToClear = playoutItems
            .Filter(pi => pi.FillerKind == FillerKind.None)
            .ToList();

        PlayoutItem lastItem = playoutItemsToClear.MaxBy(pi => pi.FinishOffset);

        // if we've finished the duration or are in offline tail mode with no fallback, keep guide finish on the last item 
        if (nextState.DurationFinish.IsNone && (scheduleItem.TailMode != TailMode.Offline || hasFallback))
        {
            playoutItemsToClear.Remove(lastItem);
        }

        foreach (PlayoutItem item in playoutItemsToClear)
        {
            item.GuideFinish = null;
        }

        nextState = nextState with { NextGuideGroup = nextState.IncrementGuideGroup };

        return Tuple(nextState, playoutItems);
    }
}
