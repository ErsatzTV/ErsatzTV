using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatSrt : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => ["-f", "srt"];
    public FrameState NextState(FrameState currentState) => currentState;
}
