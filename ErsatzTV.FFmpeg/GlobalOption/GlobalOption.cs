using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.GlobalOption;

public abstract class GlobalOption : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public abstract string[] GlobalOptions { get; }
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public virtual FrameState NextState(FrameState currentState) => currentState;
}
