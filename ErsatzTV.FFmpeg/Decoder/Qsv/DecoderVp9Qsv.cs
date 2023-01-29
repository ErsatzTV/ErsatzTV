using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderVp9Qsv : DecoderBase
{
    public override string Name => "vp9_qsv";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

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
