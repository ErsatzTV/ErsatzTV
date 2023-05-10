namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg2Cuvid : CuvidDecoder
{
    private readonly bool _contentIsInterlaced;

    public DecoderMpeg2Cuvid(HardwareAccelerationMode hardwareAccelerationMode, bool contentIsInterlaced)
        : base(hardwareAccelerationMode) =>
        _contentIsInterlaced = contentIsInterlaced;

    public override string Name => "mpeg2_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation =>
        _contentIsInterlaced || HardwareAccelerationMode == HardwareAccelerationMode.None
            ? FrameDataLocation.Software
            : FrameDataLocation.Hardware;
}
