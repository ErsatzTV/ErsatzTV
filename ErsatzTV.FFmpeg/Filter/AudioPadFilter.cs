using System.Globalization;

namespace ErsatzTV.FFmpeg.Filter;

public class AudioPadFilter : BaseFilter
{
    private readonly TimeSpan _wholeDuration;

    public AudioPadFilter(TimeSpan wholeDuration) => _wholeDuration = wholeDuration;

    public override string Filter
    {
        get
        {
            var durationString = _wholeDuration.TotalMilliseconds.ToString(NumberFormatInfo.InvariantInfo);
            return $"apad=whole_dur={durationString}ms";
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
