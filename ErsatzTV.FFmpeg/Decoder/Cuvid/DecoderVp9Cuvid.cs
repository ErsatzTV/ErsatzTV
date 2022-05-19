namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVp9Cuvid : DecoderBase
{
    public override string Name => "vp9_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }
}
