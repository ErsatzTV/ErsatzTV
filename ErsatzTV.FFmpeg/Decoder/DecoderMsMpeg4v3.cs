namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMsMpeg4V3 : DecoderBase
{
    public override string Name => "msmpeg4v3";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
