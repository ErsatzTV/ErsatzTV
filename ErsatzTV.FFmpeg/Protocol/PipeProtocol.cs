using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Protocol;

public class PipeProtocol : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => new[] { "pipe:1" };

    public FrameState NextState(FrameState currentState) => currentState;
}
