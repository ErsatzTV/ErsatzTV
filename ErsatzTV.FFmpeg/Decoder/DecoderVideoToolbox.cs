namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVideoToolbox : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override string Name => "implicit_videotoolbox";
    public override IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();
}
