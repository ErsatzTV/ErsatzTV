using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderVaapi : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override string Name => "implicit_vaapi";

    public override IList<string> InputOptions(InputFile inputFile) =>
        new List<string> { "-hwaccel_output_format", "vaapi" };

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat =>
            {
                return pixelFormat.BitDepth == 8
                    ? nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) }
                    : nextState with { PixelFormat = new PixelFormatVaapi(pixelFormat.Name) };
            },
            () => nextState);
    }
}
