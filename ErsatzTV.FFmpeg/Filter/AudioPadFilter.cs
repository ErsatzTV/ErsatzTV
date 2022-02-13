using System.Globalization;

namespace ErsatzTV.FFmpeg.Filter;

public class AudioPadFilter : IPipelineFilterStep
{
    private readonly TimeSpan _wholeDuration;

    public AudioPadFilter(TimeSpan wholeDuration)
    {
        _wholeDuration = wholeDuration;
    }

    public StreamKind StreamKind => StreamKind.Audio;

    public string Filter
    {
        get
        {
            var durationString = _wholeDuration.TotalMilliseconds.ToString(NumberFormatInfo.InvariantInfo);
            return $"apad=whole_dur={durationString}ms";
        }
    }

    public FrameState NextState(FrameState currentState) => currentState with { AudioDuration = _wholeDuration };
}
