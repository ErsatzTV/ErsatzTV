using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Format;

public static class AvailablePixelFormats
{
    public static Option<IPixelFormat> ForPixelFormat(string pixelFormat, ILogger logger) =>
        pixelFormat switch
        {
            PixelFormat.YUV420P => new PixelFormatYuv420P(),
            PixelFormat.YUV420P10LE => new PixelFormatYuv420P10Le(),
            PixelFormat.YUVJ420P => new PixelFormatYuvJ420P(),
            PixelFormat.YUV444P => new PixelFormatYuv444P(),
            PixelFormat.YUVA420P => new PixelFormatYuva420P(),
            _ => LogUnknownPixelFormat(pixelFormat, logger)
        };

    private static Option<IPixelFormat> LogUnknownPixelFormat(string pixelFormat, ILogger logger)
    {
        logger.LogWarning("Unexpected pixel format {PixelFormat} may have playback issues", pixelFormat);
        return Option<IPixelFormat>.None;
    }
}
