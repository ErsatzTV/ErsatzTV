namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVc1 : DecoderBase
{
    public override string Name => "vc1";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
