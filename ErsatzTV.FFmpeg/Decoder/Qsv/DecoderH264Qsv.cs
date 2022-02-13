using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderH264Qsv : DecoderBase
{
    public override string Name => "h264_qsv";

    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override FrameState NextState(FrameState currentState) => base.NextState(currentState) with
    {
        PixelFormat = new PixelFormatNv12(currentState.PixelFormat.Name) 
    };
}
