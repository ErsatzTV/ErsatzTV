namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderH264Cuvid : DecoderBase
{
    private readonly FFmpegState _ffmpegState;

    public DecoderH264Cuvid(FFmpegState ffmpegState) => _ffmpegState = ffmpegState;

    public override string Name => "h264_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation =>
        _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None
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
