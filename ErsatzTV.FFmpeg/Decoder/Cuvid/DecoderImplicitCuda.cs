using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderImplicitCuda : DecoderBase
{
    protected override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;

    public override string Name => "implicit_cuda";

    public override string[] InputOptions(InputFile inputFile) =>
    [
        "-hwaccel_output_format",
        "cuda"
    ];

    public override FrameState NextState(FrameState currentState)
    {
        FrameState nextState = base.NextState(currentState);

        return currentState.PixelFormat.Match(
            pixelFormat => pixelFormat.BitDepth == 8
                ? nextState with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) }
                : nextState with { PixelFormat = new PixelFormatCuda(pixelFormat.Name, 10) },
            () => nextState);
    }
}
