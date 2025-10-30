using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderAacLatm : IDecoder
{
    public string Name => "aac_latm";
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => [];

    public string[] InputOptions(InputFile inputFile) => ["-c:a", "aac_latm"];

    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => true;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;
}
