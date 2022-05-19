namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuv444P : IPixelFormat
{
    public string Name => PixelFormat.YUV444P;
    public string FFmpegName => FFmpegFormat.YUV444P;
}
