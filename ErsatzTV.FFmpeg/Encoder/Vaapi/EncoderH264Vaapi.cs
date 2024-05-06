using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderH264Vaapi(Option<string> maybeVideoProfile, RateControlMode rateControlMode) : EncoderBase
{
    public override string Name => "h264_vaapi";

    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string>(base.OutputOptions);

            if (rateControlMode == RateControlMode.CQP)
            {
                result.Add("-rc_mode");
                result.Add("1");
            }

            result.Add("-sei");
            result.Add("-a53_cc");

            foreach (string videoProfile in maybeVideoProfile)
            {
                result.Add("-profile:v");
                result.Add(videoProfile.ToLowerInvariant());
            }

            return result.ToArray();
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
        // don't change the frame data location
    };
}
