using LanguageExt;

namespace ErsatzTV.FFmpeg.Format;

public static class AvailablePixelFormats
{
    public static IPixelFormat ForPixelFormat(string pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.YUV420P => new PixelFormatYuv420P(),
            PixelFormat.YUV420P10LE => new PixelFormatYuv420P10Le(),
            PixelFormat.YUVJ420P => new PixelFormatYuvJ420P(),
            PixelFormat.YUV444P => new PixelFormatYuv444P(),
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null)
        };
    }
}
