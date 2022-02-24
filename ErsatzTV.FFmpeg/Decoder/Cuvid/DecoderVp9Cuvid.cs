namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVp9Cuvid : DecoderBase
{
    public override string Name => "vp9_cuvid";
    public override IList<string> VideoInputOptions(VideoInputFile videoInputFile)
    {
        IList<string> result = base.VideoInputOptions(videoInputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
