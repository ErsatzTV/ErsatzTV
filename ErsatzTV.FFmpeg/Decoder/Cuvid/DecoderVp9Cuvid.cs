namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVp9Cuvid : CuvidDecoder
{
    public DecoderVp9Cuvid(HardwareAccelerationMode hardwareAccelerationMode) : base(hardwareAccelerationMode)
    {
    }

    public override string Name => "vp9_cuvid";
}
