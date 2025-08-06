using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class CopyTimestampInputOption : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];

    public string[] InputOptions(InputFile inputFile) => ["-copyts"];

    public string[] FilterOptions => [];
    public string[] OutputOptions => [];
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => true;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;
}
