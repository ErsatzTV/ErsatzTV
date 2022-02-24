namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderImplicit : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override string Name => string.Empty;
    public override IList<string> VideoInputOptions(VideoInputFile videoInputFile) => Array.Empty<string>();
}
