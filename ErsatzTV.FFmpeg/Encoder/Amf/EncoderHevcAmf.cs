using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Amf;

public class EncoderHevcAmf : EncoderBase
{
    public override string Name => "hevc_amf";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
