using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderDtsCoreOnly : IDecoder
{
    public string Name => "dts";
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
