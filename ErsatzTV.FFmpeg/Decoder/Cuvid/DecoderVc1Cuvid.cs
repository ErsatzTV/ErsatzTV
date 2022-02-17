namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVc1Cuvid : DecoderBase
{
    public override string Name => "vc1_cuvid";
    public override IList<string> InputOptions
    {
        get
        {
            IList<string> result =  base.InputOptions;

            result.Add("-hwaccel_output_format");
            result.Add("cuda");

            return result;
        }
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
