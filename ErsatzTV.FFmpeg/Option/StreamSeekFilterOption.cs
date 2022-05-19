using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class StreamSeekFilterOption : IPipelineStep
{
    private readonly TimeSpan _start;

    public StreamSeekFilterOption(TimeSpan start) => _start = start;

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();

    public IList<string> FilterOptions => new List<string> { "-ss", $"{_start:c}" };
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
