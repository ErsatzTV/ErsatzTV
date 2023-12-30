using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderHevcVaapi : EncoderBase
{
    private readonly RateControlMode _rateControlMode;

    public EncoderHevcVaapi(RateControlMode rateControlMode) => _rateControlMode = rateControlMode;

    public override string Name => "hevc_vaapi";

    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string>(base.OutputOptions);

            if (_rateControlMode == RateControlMode.CQP)
            {
                result.Add("-rc_mode");
                result.Add("1");
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
