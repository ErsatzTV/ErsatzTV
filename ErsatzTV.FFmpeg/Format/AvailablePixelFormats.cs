﻿namespace ErsatzTV.FFmpeg.Format;

public static class AvailablePixelFormats
{
    public static IPixelFormat ForPixelFormat(string pixelFormat)
    {
        return pixelFormat switch
        {
            PixelFormat.YUV420P => new PixelFormatYuv420P(),
            PixelFormat.YUV420P10LE => new PixelFormatYuv420P10Le(),
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null)
        };
    }
}
