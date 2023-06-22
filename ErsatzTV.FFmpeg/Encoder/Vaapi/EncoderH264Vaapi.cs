using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderH264Vaapi : EncoderBase
{
    private readonly RateControlMode _rateControlMode;

    public EncoderH264Vaapi(RateControlMode rateControlMode) => _rateControlMode = rateControlMode;
    
    public override string Name => "h264_vaapi";
    
    public override StreamKind Kind => StreamKind.Video;
    
    public override IList<string> OutputOptions
    {
        get
        {
            IList<string> result = base.OutputOptions;

            if (_rateControlMode == RateControlMode.CQP)
            {
                result.Add("-rc_mode");
                result.Add("1");
            }
            
            return result;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
        // don't change the frame data location
    };
}
