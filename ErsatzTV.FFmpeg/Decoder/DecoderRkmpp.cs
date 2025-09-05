using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderRkmpp : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;

    public override string Name => "implicit_rkmpp";
    public override string[] InputOptions(InputFile inputFile) => Array.Empty<string>();
    
    /*
    public override string[] InputOptions(InputFile inputFile) =>
        new[] { "-hwaccel_output_format", "drm_prime" };
    */

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat => pixelFormat.BitDepth == 8
                ? nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) }
                : nextState with { PixelFormat = new PixelFormatNv15(pixelFormat.Name) },
            () => nextState);
    }
}