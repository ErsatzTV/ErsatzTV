using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputOption;

public class TimeLimitOutputOption : IPipelineStep
{
    private readonly TimeSpan _finish;

    public TimeLimitOutputOption(TimeSpan finish) => _finish = finish;

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => new[] { "-t", $"{_finish:c}" };
    public FrameState NextState(FrameState currentState) => currentState;
}
