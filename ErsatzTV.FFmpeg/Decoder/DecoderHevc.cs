namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderHevc : DecoderBase
{
    public override string Name => "hevc";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
