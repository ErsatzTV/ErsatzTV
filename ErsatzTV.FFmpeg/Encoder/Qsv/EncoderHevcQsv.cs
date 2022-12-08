using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderHevcQsv : EncoderBase
{
    public override string Name => "hevc_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override IList<string> OutputOptions =>
        new[] { "-c:v", "hevc_qsv", "-low_power", "0", "-look_ahead", "0" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
