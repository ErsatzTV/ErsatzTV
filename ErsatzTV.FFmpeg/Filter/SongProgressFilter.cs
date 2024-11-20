namespace ErsatzTV.FFmpeg.Filter;

public class SongProgressFilter(FrameSize frameSize, Option<TimeSpan> maybeDuration) : BaseFilter
{
    public override string Filter
    {
        get
        {
            foreach (TimeSpan duration in maybeDuration)
            {
                int width = frameSize.Width;
                double seconds = duration.TotalSeconds;
                return $"loop=-1:1[i],color=c=white:s={width}x10[bar];[i][bar]overlay=-w+(w/{seconds})*t:H-h:shortest=1";
            }

            return string.Empty;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
