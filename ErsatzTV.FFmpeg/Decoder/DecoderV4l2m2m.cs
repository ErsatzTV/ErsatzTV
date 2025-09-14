namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderV4l2m2m : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override string Name => "implicit_v4l2m2m";
    public override string[] InputOptions(InputFile inputFile) => [];
}
