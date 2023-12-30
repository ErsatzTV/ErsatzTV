using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class DoNotIgnoreLoopInputOption : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();

    public string[] InputOptions(InputFile inputFile) => new[] { "-ignore_loop", "0" };

    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => videoInputFile.VideoStreams.All(s => s.StillImage == false);

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;
}
