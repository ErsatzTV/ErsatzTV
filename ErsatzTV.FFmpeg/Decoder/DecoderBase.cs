using ErsatzTV.FFmpeg.Environment;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public abstract class DecoderBase : IDecoder
{
    protected abstract FrameDataLocation OutputFrameDataLocation { get; }
    public EnvironmentVariable[] EnvironmentVariables => [];
    public string[] GlobalOptions => [];
    public virtual string[] InputOptions(InputFile inputFile) => ["-c:v", Name];
    public string[] FilterOptions => [];
    public string[] OutputOptions => [];

    public virtual FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = OutputFrameDataLocation };

    public abstract string Name { get; }
    public bool AppliesTo(AudioInputFile audioInputFile) => false;

    public bool AppliesTo(VideoInputFile videoInputFile) => true;

    public bool AppliesTo(ConcatInputFile concatInputFile) => false;

    public bool AppliesTo(GraphicsEngineInput graphicsEngineInput) => false;

    protected static int InputBitDepth(InputFile inputFile)
    {
        var bitDepth = 8;

        if (inputFile is VideoInputFile videoInputFile)
        {
            foreach (VideoStream videoStream in videoInputFile.VideoStreams.HeadOrNone())
            {
                foreach (IPixelFormat pixelFormat in videoStream.PixelFormat)
                {
                    bitDepth = pixelFormat.BitDepth;
                }
            }
        }

        return bitDepth;
    }
}
