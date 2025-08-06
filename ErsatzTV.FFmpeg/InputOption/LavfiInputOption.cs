using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class LavfiInputOption : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];

    public string[] InputOptions(InputFile inputFile) => ["-f", "lavfi"];

    public string[] FilterOptions => [];
    public string[] OutputOptions => [];
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;
}
