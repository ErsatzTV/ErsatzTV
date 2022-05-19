namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVp9 : DecoderBase
{
    public override string Name => "vp9";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
