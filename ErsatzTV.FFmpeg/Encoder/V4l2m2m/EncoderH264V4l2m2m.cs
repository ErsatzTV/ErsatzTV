using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.V4l2m2m;

public class EncoderH264V4l2m2m : EncoderBase
{
    public override string Name => "h264_v4l2m2m";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions =>
    [
        "-c:v", Name,
        "-num_capture_buffers", "16"
    ];

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
