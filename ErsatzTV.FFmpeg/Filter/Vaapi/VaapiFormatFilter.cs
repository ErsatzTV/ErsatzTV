using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Filter.Vaapi;

public class VaapiFormatFilter : BaseFilter
{
    private readonly IPixelFormat _pixelFormat;

    public VaapiFormatFilter(IPixelFormat pixelFormat) => _pixelFormat = pixelFormat;

    public override FrameState NextState(FrameState currentState) => currentState with { PixelFormat = Some(_pixelFormat) };

    public override string Filter => $"scale_vaapi=format={_pixelFormat.FFmpegName}";
}
