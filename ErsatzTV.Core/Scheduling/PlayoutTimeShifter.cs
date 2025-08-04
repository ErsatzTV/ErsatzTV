using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class PlayoutTimeShifter(IFFmpegSegmenterService segmenterService, ILogger<PlayoutTimeShifter> logger)
    : IPlayoutTimeShifter
{
    public void TimeShift(Playout playout, DateTimeOffset now, bool force)
    {
        if (playout.Channel.PlayoutMode is not ChannelPlayoutMode.OnDemand)
        {
            return;
        }

        if (!force && segmenterService.IsActive(playout.Channel.Number))
        {
            logger.LogDebug(
                "Will not time shift on demand playout that is active for channel {Number} - {Name}",
                playout.Channel.Number,
                playout.Channel.Name);

            return;
        }

        if (playout.Items.Count == 0)
        {
            logger.LogDebug(
                "Unable to time shift empty playout for channel {Number} - {Name}",
                playout.Channel.Number,
                playout.Channel.Name);

            return;
        }

        if (playout.OnDemandCheckpoint is null)
        {
            logger.LogDebug(
                "Time shifting unwatched playout for channel {Number} - {Name}",
                playout.Channel.Number,
                playout.Channel.Name);

            playout.OnDemandCheckpoint = playout.Items.Min(p => p.StartOffset);
        }

        TimeSpan toOffset = now - playout.OnDemandCheckpoint.IfNone(now);

        logger.LogDebug(
            "Time shifting playout for channel {Number} - {Name} forward by {Time}",
            playout.Channel.Number,
            playout.Channel.Name,
            toOffset);

        // time shift items
        foreach (PlayoutItem playoutItem in playout.Items)
        {
            playoutItem.Start += toOffset;
            playoutItem.Finish += toOffset;

            if (playoutItem.GuideStart.HasValue)
            {
                playoutItem.GuideStart += toOffset;
            }

            if (playoutItem.GuideFinish.HasValue)
            {
                playoutItem.GuideFinish += toOffset;
            }
        }

        // time shift anchors
        foreach (PlayoutProgramScheduleAnchor anchor in playout.ProgramScheduleAnchors)
        {
            if (anchor.AnchorDate.HasValue)
            {
                anchor.AnchorDate += toOffset;
            }
        }

        // time shift anchor
        if (playout.Anchor is not null)
        {
            playout.Anchor.NextStart += toOffset;
        }

        playout.OnDemandCheckpoint = now;
    }
}
