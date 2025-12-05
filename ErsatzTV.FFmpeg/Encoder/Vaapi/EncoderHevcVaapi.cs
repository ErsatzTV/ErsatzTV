using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi(RateControlMode rateControlMode, bool packedHeaderMisc) : EncoderBase
{
    public override string Name => "hevc_vaapi";

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

            if (packedHeaderMisc)
            {
                result.Add("-sei");
                result.Add("-a53_cc");
            }

            return result.ToArray();
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
        // don't change the frame data location
    };
}
