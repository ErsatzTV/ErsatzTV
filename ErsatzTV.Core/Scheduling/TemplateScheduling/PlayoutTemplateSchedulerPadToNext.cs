using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateSchedulerPadToNext : PlayoutTemplateScheduler
{
    public static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        PlayoutTemplatePadToNextItem padToNext,
        IMediaCollectionEnumerator enumerator,
        Option<IMediaCollectionEnumerator> fallbackEnumerator)
    {
        int currentMinute = currentTime.Minute;

        int targetMinute = (currentMinute + padToNext.PadToNext - 1) / padToNext.PadToNext * padToNext.PadToNext;

        DateTimeOffset almostTargetTime =
            currentTime - TimeSpan.FromMinutes(currentMinute) + TimeSpan.FromMinutes(targetMinute);

        var targetTime = new DateTimeOffset(
            almostTargetTime.Year,
            almostTargetTime.Month,
            almostTargetTime.Day,
            almostTargetTime.Hour,
            almostTargetTime.Minute,
            0,
            almostTargetTime.Offset);

        // ensure filler works for content less than one minute
        if (targetTime <= currentTime)
            targetTime = targetTime.AddMinutes(padToNext.PadToNext);

        int discardAttempts = padToNext.DiscardAttempts;
        bool done = false;
        TimeSpan remainingToFill = targetTime - currentTime;
        while (!done && enumerator.Current.IsSome && remainingToFill > TimeSpan.Zero)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = currentTime.UtcDateTime,
                    Finish = currentTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    //GuideGroup = playoutBuilderState.NextGuideGroup,
                    //FillerKind = fillerKind,
                    //DisableWatermarks = !allowWatermarks
                };

                if (remainingToFill - itemDuration >= TimeSpan.Zero)
                {
                    remainingToFill -= itemDuration;
                    currentTime += itemDuration;

                    playout.Items.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else if (discardAttempts > 0)
                {
                    // item won't fit; try the next one
                    discardAttempts--;
                    enumerator.MoveNext();
                }
                else if (padToNext.Trim)
                {
                    // trim item to exactly fit
                    remainingToFill = TimeSpan.Zero;
                    currentTime = targetTime;

                    playoutItem.Finish = targetTime.UtcDateTime;
                    playoutItem.OutPoint = playoutItem.Finish - playoutItem.Start;

                    playout.Items.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else if (fallbackEnumerator.IsSome)
                {
                    foreach (IMediaCollectionEnumerator fallback in fallbackEnumerator)
                    {
                        remainingToFill = TimeSpan.Zero;
                        done = true;

                        // replace with fallback content
                        foreach (MediaItem fallbackItem in fallback.Current)
                        {
                            playoutItem.MediaItemId = fallbackItem.Id;
                            playoutItem.Finish = targetTime.UtcDateTime;
                            playoutItem.FillerKind = FillerKind.Fallback;

                            playout.Items.Add(playoutItem);
                            fallback.MoveNext();
                        }
                    }
                }
                else
                {
                    // item won't fit; we're done
                    done = true;
                }
            }
        }

        return targetTime;
    }
}
