namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuv444P10Le : IPixelFormat
{
    public string Name => PixelFormat.YUV444P10LE;
    public string FFmpegName => FFmpegFormat.P010LE;
}
