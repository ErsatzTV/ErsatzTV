namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderHevc : DecoderBase
{
    public override string Name => "hevc";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
