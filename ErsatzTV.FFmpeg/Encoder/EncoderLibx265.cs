using ErsatzTV.FFmpeg.Filter;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx265(FrameState currentState, Option<string> maybeVideoPreset) : EncoderBase
{
    public override string Filter => new HardwareDownloadFilter(currentState).Filter;

    // TODO: is tag:v needed for mpegts?
    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string>
            {
                "-c:v", Name,
                "-tag:v", "hvc1",
                "-x265-params", "log-level=error"
            };

            foreach (string videoPreset in maybeVideoPreset)
            {
                if (!string.IsNullOrWhiteSpace(videoPreset))
                {
                    result.Add("-preset:v");
                    result.Add(videoPreset);
                }
            }

            return result.ToArray();
        }
    }

    public override string Name => "libx265";
    public override StreamKind Kind => StreamKind.Video;

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoFormat = VideoFormat.Hevc };
}
