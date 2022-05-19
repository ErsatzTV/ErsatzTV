using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public abstract class BaseFilter : IPipelineFilterStep
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public virtual IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public virtual IList<string> FilterOptions => Array.Empty<string>();
    public virtual IList<string> OutputOptions => Array.Empty<string>();
    public abstract FrameState NextState(FrameState currentState);

    public abstract string Filter { get; }
}
