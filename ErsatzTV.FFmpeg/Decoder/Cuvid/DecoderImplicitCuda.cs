namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderImplicitCuda : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    public override string Name => string.Empty;

    public override string[] InputOptions(InputFile inputFile) =>
        new[]
        {
            "-hwaccel_output_format",
            "cuda"
        };
}
