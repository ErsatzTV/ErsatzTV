namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public abstract class CuvidDecoder : DecoderBase
{
    protected CuvidDecoder(HardwareAccelerationMode hardwareAccelerationMode)
    {
        HardwareAccelerationMode = hardwareAccelerationMode;
    }

    public HardwareAccelerationMode HardwareAccelerationMode { get; set; }

    protected override FrameDataLocation OutputFrameDataLocation =>
        HardwareAccelerationMode == HardwareAccelerationMode.None
            ? FrameDataLocation.Software
            : FrameDataLocation.Hardware;
    
    public override IList<string> InputOptions(InputFile inputFile)
    {
        IList<string> result = base.InputOptions(inputFile);

        if (HardwareAccelerationMode != HardwareAccelerationMode.None)
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
