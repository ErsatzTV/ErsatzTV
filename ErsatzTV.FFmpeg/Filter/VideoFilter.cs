using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Filter;

public class VideoFilter : IPipelineStep
{
    private readonly IEnumerable<IPipelineFilterStep> _filterSteps;

    public VideoFilter(IEnumerable<IPipelineFilterStep> filterSteps) => _filterSteps = filterSteps;

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Arguments();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    private IList<string> Arguments() =>
        new List<string>
        {
            "-vf",
            string.Join(",", _filterSteps.Map(fs => fs.Filter))
        };
}
