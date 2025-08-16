using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputOption;

public class TimeLimitOutputOption(TimeSpan finish) : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => ["-t", $"{(int)finish.TotalHours:00}:{finish:mm}:{finish:ss\\.fffffff}"];
    public FrameState NextState(FrameState currentState) => currentState;
}
