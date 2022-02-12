using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx265 : IEncoder
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;

    public IList<string> InputOptions => Array.Empty<string>();

    // TODO: is tag:v needed for mpegts?
    public IList<string> OutputOptions => new List<string> { "-c:v", Name, "-tag:v", "hvc1" };
    public FrameState NextState(FrameState currentState) => currentState with { VideoFormat = VideoFormat.Hevc };
    public string Name => "libx265";
    public StreamKind Kind => StreamKind.Video;
}
