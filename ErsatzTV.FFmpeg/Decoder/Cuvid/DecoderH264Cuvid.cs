namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderH264Cuvid : DecoderBase
{
    private readonly FrameState _desiredState;

    public DecoderH264Cuvid(FrameState desiredState)
    {
        _desiredState = desiredState;
    }

    public override string Name => "h264_cuvid";

    public override IList<string> InputOptions
    {
        get
        {
            IList<string> result =  base.InputOptions;

            if (_desiredState.Deinterlaced)
            {
                result.Add("-deint");
                result.Add("2");
            }

            result.Add("-hwaccel_output_format");
            result.Add("cuda");

            return result;
        }
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    
    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = base.NextState(currentState);
        return _desiredState.Deinterlaced ? result with { Deinterlaced = true } : result;
    }
}
