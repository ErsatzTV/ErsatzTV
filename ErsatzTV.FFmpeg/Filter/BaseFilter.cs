using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public abstract class BaseFilter : IPipelineFilterStep
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public virtual string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public virtual string[] FilterOptions => Array.Empty<string>();
    public virtual string[] OutputOptions => Array.Empty<string>();
    public abstract FrameState NextState(FrameState currentState);

    public abstract string Filter { get; }
}
