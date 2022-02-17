namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderHevcCuvid : DecoderBase
{
    public override string Name => "hevc_cuvid";
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
