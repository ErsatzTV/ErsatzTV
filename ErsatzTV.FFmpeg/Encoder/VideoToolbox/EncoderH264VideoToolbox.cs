using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.VideoToolbox;

public class EncoderH264VideoToolbox : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "h264_videotoolbox";
    public override StreamKind Kind => StreamKind.Video;
}
