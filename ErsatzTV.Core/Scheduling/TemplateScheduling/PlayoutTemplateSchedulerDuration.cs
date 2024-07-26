using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using TimeSpanParserUtil;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateSchedulerDuration : PlayoutTemplateScheduler
{
    public static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        PlayoutTemplateDurationItem duration,
        IMediaCollectionEnumerator enumerator,
        Option<IMediaCollectionEnumerator> fallbackEnumerator)
    {
        // TODO: move to up-front validation somewhere
        if (!TimeSpanParser.TryParse(duration.Duration, out TimeSpan timeSpan))
        {
            return currentTime;
        }

        DateTimeOffset targetTime = currentTime.Add(timeSpan);

        return Schedule(
            playout,
            currentTime,
            targetTime,
            duration.DiscardAttempts,
            duration.Trim,
            enumerator,
            fallbackEnumerator);
    }

    protected static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        DateTimeOffset targetTime,
        int discardAttempts,
        bool trim,
        IMediaCollectionEnumerator enumerator,
        Option<IMediaCollectionEnumerator> fallbackEnumerator)
    {
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
                else if (trim)
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
