namespace ErsatzTV.FFmpeg.Option;

public class StreamSeekInputOption : IPipelineStep
{
    private readonly TimeSpan _start;

    public StreamSeekInputOption(TimeSpan start)
    {
        _start = start;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions =>
        _start == TimeSpan.Zero ? Array.Empty<string>() : new List<string> { "-ss", $"{_start:c}" };
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState with { Start = _start };
}
