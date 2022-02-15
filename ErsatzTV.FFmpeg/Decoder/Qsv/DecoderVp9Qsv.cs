namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderVp9Qsv : DecoderBase
{
    public override string Name => "vp9_qsv";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
