namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderVc1Qsv : DecoderBase
{
    public override string Name => "vc1_qsv";

    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
