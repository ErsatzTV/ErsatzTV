namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderVc1Cuvid : DecoderBase
{
    private readonly FFmpegState _ffmpegState;

    public DecoderVc1Cuvid(FFmpegState ffmpegState) => _ffmpegState = ffmpegState;

    public override string Name => "vc1_cuvid";

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
