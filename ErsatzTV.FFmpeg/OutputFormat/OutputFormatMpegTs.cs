using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatMpegTs : IPipelineStep
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();

    // always force an initial discontinuity
    public IList<string> OutputOptions =>
        new List<string> { "-f", "mpegts", "-mpegts_flags", "+initial_discontinuity" };

    public FrameState NextState(FrameState currentState) => currentState;
}
