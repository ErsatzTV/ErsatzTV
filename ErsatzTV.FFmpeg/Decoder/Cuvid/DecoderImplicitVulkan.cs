namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderImplicitVulkan : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    public override string Name => string.Empty;

    public override string[] InputOptions(InputFile inputFile) =>
    [
        "-hwaccel_output_format",
        "vulkan"
    ];
}
