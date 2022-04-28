using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter;

public class PixelFormatFilter : BaseFilter
{
    private readonly IPixelFormat _pixelFormat;

    public PixelFormatFilter(IPixelFormat pixelFormat) => _pixelFormat = pixelFormat;

    public override string Filter => $"format={_pixelFormat.FFmpegName}";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { PixelFormat = Some(_pixelFormat) };
}
