using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderMpeg2Video : EncoderBase
{
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override FrameState NextState(FrameState currentState) => currentState with { VideoFormat = VideoFormat.Mpeg2Video };
    public override string Name => "mpeg2video";
    public override StreamKind Kind => StreamKind.Video;
}
