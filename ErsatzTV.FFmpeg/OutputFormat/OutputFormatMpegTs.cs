using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatMpegTs : IPipelineStep
{
    private readonly bool _initialDiscontinuity;

    public OutputFormatMpegTs(bool initialDiscontinuity = true)
    {
        _initialDiscontinuity = initialDiscontinuity;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();

    public IList<string> OutputOptions => _initialDiscontinuity
        ? new List<string> { "-f", "mpegts", "-mpegts_flags", "+initial_discontinuity" }
        : new List<string> { "-f", "mpegts" };

    public FrameState NextState(FrameState currentState) => currentState;
}
