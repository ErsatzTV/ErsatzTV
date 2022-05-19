namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg2Video : DecoderBase
{
    public override string Name => "mpeg2video";
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
