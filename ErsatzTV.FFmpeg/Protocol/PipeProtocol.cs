using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Protocol;

public class PipeProtocol : IPipelineStep
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "pipe:1" };

    public FrameState NextState(FrameState currentState) => currentState;
}
