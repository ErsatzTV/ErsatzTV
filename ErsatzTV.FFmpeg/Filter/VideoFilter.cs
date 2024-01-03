using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public class VideoFilter : IPipelineStep
{
    private readonly IEnumerable<IPipelineFilterStep> _filterSteps;

    public VideoFilter(IEnumerable<IPipelineFilterStep> filterSteps) => _filterSteps = filterSteps;

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Arguments();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    private string[] Arguments() =>
    [
        "-vf",
            string.Join(",", _filterSteps.Map(fs => fs.Filter))
    ];
}
