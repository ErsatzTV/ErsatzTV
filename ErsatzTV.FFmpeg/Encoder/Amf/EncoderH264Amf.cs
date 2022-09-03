using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Amf;

public class EncoderH264Amf : EncoderBase
{
    public override string Name => "h264_amf";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
