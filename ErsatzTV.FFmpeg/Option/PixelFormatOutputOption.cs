using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Option;

public class PixelFormatOutputOption : OutputOption
{
    private readonly IPixelFormat _pixelFormat;

    public PixelFormatOutputOption(IPixelFormat pixelFormat)
    {
        _pixelFormat = pixelFormat;
    }

    public override IList<string> OutputOptions => new List<string>
    {
        "-pix_fmt", _pixelFormat.FFmpegName
    };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { PixelFormat = Some(_pixelFormat) };
}
