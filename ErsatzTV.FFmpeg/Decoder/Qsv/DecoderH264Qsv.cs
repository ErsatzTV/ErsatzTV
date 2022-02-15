using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderH264Qsv : DecoderBase
{
    public override string Name => "h264_qsv";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat => nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) },
            () => nextState);
    }
}
