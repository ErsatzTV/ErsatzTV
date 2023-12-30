using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx265 : EncoderBase
{
    private readonly FrameState _currentState;

    public EncoderLibx265(FrameState currentState) => _currentState = currentState;

    public override string Filter => new HardwareDownloadFilter(_currentState).Filter;

    // TODO: is tag:v needed for mpegts?
    public override string[] OutputOptions => new[]
        { "-c:v", Name, "-tag:v", "hvc1", "-x265-params", "log-level=error" };

    public override string Name => "libx265";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoFormat = VideoFormat.Hevc };
}
