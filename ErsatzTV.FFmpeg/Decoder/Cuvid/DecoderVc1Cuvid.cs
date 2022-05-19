namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVc1Cuvid : DecoderBase
{
    public override string Name => "vc1_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }
}
