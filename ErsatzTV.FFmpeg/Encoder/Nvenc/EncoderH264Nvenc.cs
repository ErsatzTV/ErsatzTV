using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderH264Nvenc : EncoderBase
{
    public override string Name => "h264_nvenc";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
    };
}
