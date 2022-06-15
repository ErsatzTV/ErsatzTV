namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg2Cuvid : DecoderBase
{
    private readonly bool _contentIsInterlaced;
    private readonly FFmpegState _ffmpegState;

    public DecoderMpeg2Cuvid(FFmpegState ffmpegState, bool contentIsInterlaced)
    {
        _ffmpegState = ffmpegState;
        _contentIsInterlaced = contentIsInterlaced;
    }

    public override string Name => "mpeg2_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation =>
        _contentIsInterlaced || _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None
            ? FrameDataLocation.Software
            : FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        result.Add("-hwaccel_output_format");
        result.Add(_ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.None ? "cuda" : "nv12");

        return result;
    }
}
