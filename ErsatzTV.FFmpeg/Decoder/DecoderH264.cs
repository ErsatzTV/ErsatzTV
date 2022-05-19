namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderH264 : DecoderBase
{
    public override string Name => "h264";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
