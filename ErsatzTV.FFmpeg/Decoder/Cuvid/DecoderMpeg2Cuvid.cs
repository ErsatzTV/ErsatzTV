namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg2Cuvid : DecoderBase
{
    private readonly bool _contentIsInterlaced;

    public DecoderMpeg2Cuvid(bool contentIsInterlaced) => _contentIsInterlaced = contentIsInterlaced;

    public override string Name => "mpeg2_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation =>
        _contentIsInterlaced ? FrameDataLocation.Software : FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }
}
