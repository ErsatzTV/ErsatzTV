using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx264(Option<string> maybeVideoProfile, Option<string> maybeVideoPreset) : EncoderBase
{
    public override string Name => "libx264";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string>(base.OutputOptions);

            foreach (string videoProfile in maybeVideoProfile)
            {
                result.Add("-profile:v");
                result.Add(videoProfile.ToLowerInvariant());
            }

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

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoFormat = VideoFormat.H264 };
}
