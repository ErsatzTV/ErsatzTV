using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.V4l2m2m;

public class DecoderVp9V4l2m2m : DecoderBase
{
    public override string Name => "vp9_v4l2m2m";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat =>
            {
                if (pixelFormat.BitDepth == 10)
                {
                    return nextState with { PixelFormat = new PixelFormatP010() };
                }

                return nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
            },
            () => nextState);
    }
}
