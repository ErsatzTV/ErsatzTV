using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.V4l2m2m;

public class EncoderHevcV4l2m2m : EncoderBase
{
    public override string Name => "hevc_V4l2m2m";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
