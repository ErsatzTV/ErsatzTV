namespace ErsatzTV.FFmpeg.Decoder.Qsv;

public class DecoderVc1Qsv : DecoderBase
{
    public override string Name => "vc1_qsv";

    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
}
