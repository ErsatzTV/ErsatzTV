namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg4Cuvid : DecoderBase
{
    private readonly FrameState _desiredState;

    public DecoderMpeg4Cuvid(FrameState desiredState)
    {
        _desiredState = desiredState;
    }

    public override string Name => "mpeg4_cuvid";

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

    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    
    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = base.NextState(currentState);
        return _desiredState.Deinterlaced ? result with { Deinterlaced = true } : result;
    }
}
