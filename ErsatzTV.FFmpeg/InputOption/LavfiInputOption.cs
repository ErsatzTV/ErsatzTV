using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class LavfiInputOption : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();

    public string[] InputOptions(InputFile inputFile) => new[] { "-f", "lavfi" };

    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;
}
