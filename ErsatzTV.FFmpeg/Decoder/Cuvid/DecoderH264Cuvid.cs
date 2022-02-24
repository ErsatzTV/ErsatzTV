namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderH264Cuvid : DecoderBase
{
    public override string Name => "h264_cuvid";

    public override IList<string> VideoInputOptions(VideoInputFile videoInputFile)
    {
        IList<string> result = base.VideoInputOptions(videoInputFile);

        result.Add("-hwaccel_output_format");
        result.Add("cuda");

        return result;
    }

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
