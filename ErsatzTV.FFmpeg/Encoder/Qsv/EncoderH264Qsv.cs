using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv : EncoderBase
{
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };

    public override string Name => "h264_qsv";
    public override StreamKind Kind => StreamKind.Video;
}
