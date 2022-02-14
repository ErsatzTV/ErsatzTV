using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx264 : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState with { VideoFormat = VideoFormat.H264 };
    public override string Name => "libx264";
    public override StreamKind Kind => StreamKind.Video;
}
