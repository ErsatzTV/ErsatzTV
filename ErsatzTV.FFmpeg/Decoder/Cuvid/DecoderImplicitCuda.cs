namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderImplicitCuda : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    public override string Name => string.Empty;
    public override IList<string> InputOptions(InputFile inputFile) =>
        new List<string>
        {
            "-hwaccel_output_format",
            "cuda"
        };
}
