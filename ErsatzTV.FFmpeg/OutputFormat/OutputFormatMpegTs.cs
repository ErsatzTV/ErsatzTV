using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.OutputFormat;

public class OutputFormatMpegTs : IPipelineStep
{
    private readonly bool _initialDiscontinuity;

    public OutputFormatMpegTs(bool initialDiscontinuity = true) => _initialDiscontinuity = initialDiscontinuity;

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    public string[] FilterOptions => Array.Empty<string>();

    public string[] OutputOptions => _initialDiscontinuity
        ? new[] { "-f", "mpegts", "-mpegts_flags", "+initial_discontinuity" }
        : new[] { "-f", "mpegts" };

    public FrameState NextState(FrameState currentState) => currentState;
}
