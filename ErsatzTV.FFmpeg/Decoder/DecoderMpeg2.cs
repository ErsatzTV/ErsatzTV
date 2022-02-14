namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg2Video : DecoderBase
{
    public override string Name => "mpeg2video";
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
