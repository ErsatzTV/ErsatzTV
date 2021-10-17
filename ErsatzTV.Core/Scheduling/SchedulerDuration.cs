using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Scheduling
{
    public class SchedulerDuration : SchedulerBase
    {
        protected override IMediaCollectionEnumerator GetEnumerator(
            PlayoutBuilderState playoutBuilderState,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators,
            ProgramScheduleItem scheduleItem)
        {
            Option<CollectionKey> maybeTailCollectionKey = Option<CollectionKey>.None;
            if (playoutBuilderState.InDurationFiller && scheduleItem is ProgramScheduleItemDuration
            {
                TailMode: TailMode.Filler
            })
            {
                maybeTailCollectionKey = TailCollectionKeyForItem(scheduleItem);
            }

            IMediaCollectionEnumerator enumerator = collectionEnumerators[CollectionKeyForItem(scheduleItem)];
            foreach (CollectionKey tailCollectionKey in maybeTailCollectionKey)
            {
                enumerator = collectionEnumerators[tailCollectionKey];
            }

            return enumerator;
        }

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
                IsFiller = playoutBuilderState.InDurationFiller || scheduleItem.GuideMode == GuideMode.Filler
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
            PlayoutBuilderState nextState = playoutBuilderState;
            if (scheduleItem is ProgramScheduleItemDuration duration)
            {
                MediaVersion peekVersion = peekMediaItem switch
                {
                    Movie m => m.MediaVersions.Head(),
                    Episode e => e.MediaVersions.Head(),
                    MusicVideo mv => mv.MediaVersions.Head(),
                    OtherVideo ov => ov.MediaVersions.Head(),
                    _ => throw new ArgumentOutOfRangeException(nameof(peekMediaItem))
                };

                // remember when we need to finish this duration item
                if (nextState.DurationFinish.IsNone)
                {
                    nextState = nextState with
                    {
                        DurationFinish = itemStartTime + duration.PlayoutDuration,
                        CustomGroup = true
                    };
                }

                bool willNotFinishInTime =
                    nextState.CurrentTime <= nextState.DurationFinish.IfNone(SystemTime.MinValueUtc) &&
                    nextState.CurrentTime + peekVersion.Duration >
                    nextState.DurationFinish.IfNone(SystemTime.MinValueUtc);
                if (willNotFinishInTime)
                {
                    logger.LogDebug(
                        "Advancing to next schedule item after playout mode {PlayoutMode}",
                        "Duration");

                    nextState = nextState with { ScheduleItemIndex = nextState.ScheduleItemIndex + 1 };

                    if (duration.TailMode == TailMode.Offline)
                    {
                        foreach (DateTimeOffset df in nextState.DurationFinish)
                        {
                            nextState = nextState with { CurrentTime = df };
                        }
                    }

                    if (duration.TailMode != TailMode.Filler || nextState.InDurationFiller)
                    {
                        if (duration.TailMode != TailMode.None)
                        {
                            foreach (DateTimeOffset df in nextState.DurationFinish)
                            {
                                nextState = nextState with { CurrentTime = df };
                            }
                        }

                        nextState = nextState with
                        {
                            DurationFinish = None,
                            InDurationFiller = false,
                            CustomGroup = false
                        };
                    }
                    else if (duration.TailMode == TailMode.Filler &&
                             WillFinishFillerInTime(
                                 scheduleItem,
                                 nextState.CurrentTime,
                                 nextState.DurationFinish,
                                 collectionEnumerators))
                    {
                        nextState = nextState with { InDurationFiller = true };
                        foreach (DateTimeOffset df in nextState.DurationFinish)
                        {
                            foreach (PlayoutItem playoutItem in playoutItems)
                            {
                                playoutItem.GuideFinish = df.UtcDateTime;
                            }
                        }
                    }
                }
            }

            return nextState;
        }

        private static bool WillFinishFillerInTime(
            ProgramScheduleItem scheduleItem,
            DateTimeOffset currentTime,
            Option<DateTimeOffset> durationFinish,
            Dictionary<CollectionKey, IMediaCollectionEnumerator> collectionEnumerators)
        {
            Option<CollectionKey> maybeTailCollectionKey = Option<CollectionKey>.None;
            if (scheduleItem is ProgramScheduleItemDuration
            {
                TailMode: TailMode.Filler
            })
            {
                maybeTailCollectionKey = TailCollectionKeyForItem(scheduleItem);
            }

            foreach (CollectionKey collectionKey in maybeTailCollectionKey)
            {
                IMediaCollectionEnumerator enumerator = collectionEnumerators[collectionKey];
                Option<int> firstId = enumerator.Current.Map(i => i.Id);
                while (true)
                {
                    foreach (MediaItem peekMediaItem in enumerator.Current)
                    {
                        MediaVersion peekVersion = peekMediaItem switch
                        {
                            Movie m => m.MediaVersions.Head(),
                            Episode e => e.MediaVersions.Head(),
                            MusicVideo mv => mv.MediaVersions.Head(),
                            OtherVideo ov => ov.MediaVersions.Head(),
                            _ => throw new ArgumentOutOfRangeException(nameof(peekMediaItem))
                        };

                        if (currentTime + peekVersion.Duration <= durationFinish.IfNone(SystemTime.MinValueUtc))
                        {
                            return true;
                        }
                    }

                    enumerator.MoveNext();
                    if (enumerator.Current.Map(i => i.Id) == firstId)
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
