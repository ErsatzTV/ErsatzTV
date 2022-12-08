namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderHevcCuvid : CuvidDecoder
{
    public DecoderHevcCuvid(HardwareAccelerationMode hardwareAccelerationMode) : base(hardwareAccelerationMode)
    {
    }

    public override string Name => "hevc_cuvid";
}
