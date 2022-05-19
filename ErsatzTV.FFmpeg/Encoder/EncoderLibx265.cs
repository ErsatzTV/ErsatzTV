using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx265 : EncoderBase
{
    // TODO: is tag:v needed for mpegts?
    public override IList<string> OutputOptions => new List<string>
        { "-c:v", Name, "-tag:v", "hvc1", "-x265-params", "log-level=error" };

    public override string Name => "libx265";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoFormat = VideoFormat.Hevc };
}
