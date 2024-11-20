namespace ErsatzTV.FFmpeg.Filter;

public class SongProgressFilter(FrameSize frameSize, Option<TimeSpan> maybeStart, Option<TimeSpan> maybeDuration) : BaseFilter
{
    public override string Filter
    {
        get
        {
            foreach (TimeSpan duration in maybeDuration)
            {
                TimeSpan start = maybeStart.IfNone(TimeSpan.Zero);
                TimeSpan finish = start + duration;

                double width = frameSize.Width * 0.9;
                double height = frameSize.Height * 0.025;
                double seconds = duration.TotalSeconds;
                double alreadyPlayed = start.TotalSeconds / finish.TotalSeconds;
                double scale = 1 - alreadyPlayed;

                var generateWhiteBar = $"color=c=white:s={width}x{height}";
                var scaleToFullWidth = $"scale=iw*{alreadyPlayed}+iw*(t/{seconds})*{scale}:ih:eval=frame";
                var overlayBar = "overlay=W*0.05:H-h-H*0.05:shortest=1:enable='gt(t,0.1)'";

                return $"loop=-1:1[si],{generateWhiteBar},{scaleToFullWidth}[sbar];[si][sbar]{overlayBar}";
            }

            return string.Empty;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
