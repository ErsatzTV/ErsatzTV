namespace ErsatzTV.FFmpeg.Decoder.V4l2m2m;

public class DecoderMpeg2V4l2m2m : DecoderBase
{
    public override string Name => "mpeg2_v4l2m2m";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
}
