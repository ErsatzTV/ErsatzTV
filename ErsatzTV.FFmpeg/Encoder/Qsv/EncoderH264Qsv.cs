using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv(Option<string> maybeVideoProfile, Option<string> maybeVideoPreset) : EncoderBase
{
    public override string Name => "h264_qsv";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string> { "-c:v", Name, "-low_power", "0", "-look_ahead", "0" };

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

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
