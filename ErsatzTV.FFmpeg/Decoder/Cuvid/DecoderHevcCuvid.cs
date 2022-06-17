namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderHevcCuvid : DecoderBase
{
    private readonly FFmpegState _ffmpegState;

    public DecoderHevcCuvid(FFmpegState ffmpegState) => _ffmpegState = ffmpegState;

    public override string Name => "hevc_cuvid";

    protected override FrameDataLocation OutputFrameDataLocation =>
        _ffmpegState.EncoderHardwareAccelerationMode == HardwareAccelerationMode.None
            ? FrameDataLocation.Software
            : FrameDataLocation.Hardware;

    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        if (_ffmpegState.EncoderHardwareAccelerationMode != HardwareAccelerationMode.None)
        {
            result.Add("-hwaccel_output_format");
            result.Add("cuda");
        }
        else
        {
            result.Add("-hwaccel_output_format");
            result.Add(InputBitDepth(inputFile) == 10 ? "p010le" : "nv12");
        }

        return result;
    }
}
