namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderRawVideo : DecoderBase
{
    public override string Name => "rawvideo";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
