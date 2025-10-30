using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.InputOption;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderDtsCoreOnly : IInputOption
{
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => [];

    public string[] InputOptions(InputFile inputFile) => ["-c:a", "dts", "-core_only", "true"];

    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;
}
