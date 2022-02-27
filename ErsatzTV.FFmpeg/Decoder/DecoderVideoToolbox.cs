using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVideoToolbox : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public override string Name => "implicit_videotoolbox";
    public override IList<string> InputOptions(InputFile inputFile) => Array.Empty<string>();

    // public override FrameState NextState(FrameState currentState)
    // {
    //     FrameState nextState = base.NextState(currentState);
    //
    //     return currentState.PixelFormat.Match(
    //         pixelFormat => nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) },
    //         () => nextState);
    // }
}
