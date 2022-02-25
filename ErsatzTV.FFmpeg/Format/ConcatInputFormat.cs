using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Format;

public class ConcatInputFormat : IPipelineStep
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();

    public IList<string> InputOptions(InputFile inputFile) => new List<string>
    {
        "-f", "concat",
        "-safe", "0",
        "-protocol_whitelist", "file,http,tcp,https,tcp,tls",
        "-probesize", "32"
    };

    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
}
