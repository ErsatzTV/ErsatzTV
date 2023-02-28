namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderAv1Cuvid : CuvidDecoder
{
    public DecoderAv1Cuvid(HardwareAccelerationMode hardwareAccelerationMode) : base(hardwareAccelerationMode)
    {
    }

    public override string Name => "av1_cuvid";
}
