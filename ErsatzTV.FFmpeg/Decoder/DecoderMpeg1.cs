namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg1Video : DecoderBase
{
    public override string Name => "mpeg1video";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
