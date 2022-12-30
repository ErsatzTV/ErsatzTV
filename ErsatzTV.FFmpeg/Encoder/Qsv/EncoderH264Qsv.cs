using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv : EncoderBase
{
    public override string Name => "h264_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override IList<string> OutputOptions =>
        new[] { "-c:v", "h264_qsv", "-low_power", "0", "-look_ahead", "0" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
