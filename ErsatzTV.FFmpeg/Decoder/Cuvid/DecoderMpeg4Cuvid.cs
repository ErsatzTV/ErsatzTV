namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg4Cuvid : CuvidDecoder
{
    public DecoderMpeg4Cuvid(HardwareAccelerationMode hardwareAccelerationMode) : base(hardwareAccelerationMode)
    {
    }

    public override string Name => "mpeg4_cuvid";
}
