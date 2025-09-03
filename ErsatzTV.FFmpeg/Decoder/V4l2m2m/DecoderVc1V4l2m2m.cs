namespace ErsatzTV.FFmpeg.Decoder.V4l2m2m;

public class DecoderVc1V4l2m2m : DecoderBase
{
    public override string Name => "vc1_v4l2m2m";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
