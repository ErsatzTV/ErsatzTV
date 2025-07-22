using System.Globalization;

namespace ErsatzTV.FFmpeg.Filter;

public class AudioPadFilter(TimeSpan audioDuration) : BaseFilter
{
    public override string Filter => $"apad=whole_dur={audioDuration.TotalMilliseconds.ToString(NumberFormatInfo.InvariantInfo)}ms";

    public override FrameState NextState(FrameState currentState) => currentState;
}
