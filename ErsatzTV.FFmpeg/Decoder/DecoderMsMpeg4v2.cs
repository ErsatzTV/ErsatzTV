namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMsMpeg4V2 : DecoderBase
{
    public override string Name => "msmpeg4v2";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
