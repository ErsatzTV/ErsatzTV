using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderAc3Downmix(int sourceChannels, int desiredChannels) : IDecoder
{
    public string Name => "ac3";
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public string[] FilterOptions => [];
    public string[] OutputOptions => [];

    public string[] InputOptions(InputFile inputFile)
    {
        if (sourceChannels >= 2 && desiredChannels == 2)
        {
            return ["-c:a", "ac3", "-downmix", AudioLayout.Stereo];
        }

        // unsupported downmix; hopefully the layout doesn't change
        return [];
    }

    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    public bool AppliesTo(VideoInputFile videoInputFile) => false;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;

}
