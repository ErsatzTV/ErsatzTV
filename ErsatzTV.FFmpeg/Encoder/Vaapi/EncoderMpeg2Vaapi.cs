using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Vaapi;

public class EncoderMpeg2Vaapi : EncoderBase
{
    private readonly RateControlMode _rateControlMode;

    public EncoderMpeg2Vaapi(RateControlMode rateControlMode) => _rateControlMode = rateControlMode;

    public override string Name => "mpeg2_vaapi";
    
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
        VideoFormat = VideoFormat.Mpeg2Video
        // don't change the frame data location
    };
}
