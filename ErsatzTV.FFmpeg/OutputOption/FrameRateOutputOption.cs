using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputOption;

public class FrameRateOutputOption(FrameRate frameRate) : IPipelineStep
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] InputOptions(InputFile inputFile) => [];
    public string[] FilterOptions => [];

    public string[] OutputOptions => ["-r", frameRate.RFrameRate, "-vsync", "cfr"];

    public FrameState NextState(FrameState currentState) => currentState with
    {
        FrameRate = frameRate
    };
}
