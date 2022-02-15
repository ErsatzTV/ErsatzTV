namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg1Video : DecoderBase
{
    public override string Name => "mpeg1video";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
