namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderH264Cuvid : CuvidDecoder
{
    public DecoderH264Cuvid(HardwareAccelerationMode hardwareAccelerationMode) : base(hardwareAccelerationMode)
    {
    }

    public override string Name => "h264_cuvid";
}
