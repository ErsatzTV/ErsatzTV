namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderH264 : DecoderBase
{
    public override string Name => "h264";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
