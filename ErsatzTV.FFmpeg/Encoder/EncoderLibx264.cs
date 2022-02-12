using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx264 : IEncoder
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-c:v", Name };
    public FrameState NextState(FrameState currentState) => currentState with { VideoFormat = VideoFormat.H264 };
    public string Name => "libx264";
    public StreamKind Kind => StreamKind.Video;
}
