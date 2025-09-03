using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.V4l2m2m;

public class DecoderH264V4l2m2m : DecoderBase
{
    public override string Name => "h264_v4l2m2m";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat => nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) },
            () => nextState);
    }
}
