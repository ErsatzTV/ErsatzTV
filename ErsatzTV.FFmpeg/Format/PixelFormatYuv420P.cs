namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuv420P : IPixelFormat
{
    public string Name => PixelFormat.YUV420P;
    public string FFmpegName => FFmpegFormat.YUV420P;
}
