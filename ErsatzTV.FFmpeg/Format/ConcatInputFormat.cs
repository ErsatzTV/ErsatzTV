using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.InputOption;

namespace ErsatzTV.FFmpeg.Format;

public class ConcatInputFormat : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();

    public string[] InputOptions(InputFile inputFile) => new[]
    {
        "-f", "concat",
        "-safe", "0",
        "-protocol_whitelist", "file,http,tcp,https,tcp,tls",
        "-probesize", "32"
    };

    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;
    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => true;
}
