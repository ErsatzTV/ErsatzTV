namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderHevcCuvid : DecoderBase
{
    public override string Name => "hevc_cuvid";
    public override IList<string> VideoInputOptions(VideoInputFile videoInputFile)
    {
        IList<string> result = base.VideoInputOptions(videoInputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
