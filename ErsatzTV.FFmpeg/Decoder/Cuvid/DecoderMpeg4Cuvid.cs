namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg4Cuvid : DecoderBase
{
    public override string Name => "mpeg4_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }
}
