using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class TimeLimitOutputOption : IPipelineStep
{
    private readonly TimeSpan _finish;

    public TimeLimitOutputOption(TimeSpan finish)
    {
        _finish = finish;
    }

    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-t", $"{_finish:c}" };
    public FrameState NextState(FrameState currentState) => currentState with { Finish = _finish };
}
