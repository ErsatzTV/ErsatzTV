using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateSchedulerPadToNext : PlayoutTemplateScheduler
{
    public static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        PlayoutTemplatePadToNextItem padToNext,
        IMediaCollectionEnumerator enumerator)
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

        bool done = false;
        TimeSpan remainingToFill = targetTime - currentTime;
        while (!done && enumerator.Current.IsSome && remainingToFill > TimeSpan.Zero &&
               remainingToFill >= enumerator.MinimumDuration)
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
                    playout.Items.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else if (padToNext.Trim)
                {
                    // trim item to exactly fit
                    remainingToFill = TimeSpan.Zero;
                    playoutItem.Finish = targetTime.UtcDateTime;
                    playoutItem.OutPoint = playoutItem.Finish - playoutItem.Start;
                    playout.Items.Add(playoutItem);
                    enumerator.MoveNext();
                }
                else
                {
                    // item won't fit; we're done for now
                    done = true;
                }
            }
        }

        return targetTime;
    }
}
