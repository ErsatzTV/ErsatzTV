using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderAv1Qsv : EncoderBase
{
    public override string Name => "av1_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions => ["-c:v", Name, "-low_power", "0", "-look_ahead", "0"];

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Av1,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
