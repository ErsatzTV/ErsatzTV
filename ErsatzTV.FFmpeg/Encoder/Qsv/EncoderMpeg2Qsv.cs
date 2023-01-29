using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderMpeg2Qsv : EncoderBase
{
    public override string Name => "mpeg2_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override IList<string> OutputOptions =>
        new[] { "-c:v", "mpeg2_qsv", "-low_power", "0", "-look_ahead", "0" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Mpeg2Video,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
