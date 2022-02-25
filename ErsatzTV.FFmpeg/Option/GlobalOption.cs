using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public abstract class GlobalOption : IPipelineStep
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public abstract IList<string> GlobalOptions { get; }
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public virtual FrameState NextState(FrameState currentState) => currentState;
}
