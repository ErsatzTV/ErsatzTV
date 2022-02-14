namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVp9 : DecoderBase
{
    public override string Name => "vp9";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
