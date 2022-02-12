namespace ErsatzTV.FFmpeg.Option;

public class SliceOption : IPipelineStep
{
    private readonly TimeSpan? _inPoint;
    private readonly TimeSpan _duration;

    public SliceOption(TimeSpan? inPoint, TimeSpan duration)
    {
        _inPoint = inPoint;
        _duration = duration;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions =>
        _inPoint.HasValue ? new List<string> { "-ss", $"{_inPoint:c}" } : Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-t", $"{_duration:c}" };
    public FrameState NextState(FrameState currentState) => currentState;
}
