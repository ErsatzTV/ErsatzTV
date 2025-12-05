using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderAv1Vaapi(RateControlMode rateControlMode) : EncoderBase
{
    public override string Name => "av1_vaapi";

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

            return result.ToArray();
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Av1
        // don't change the frame data location
    };
}
