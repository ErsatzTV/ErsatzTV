namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuvJ420P : IPixelFormat
{
    public string Name => PixelFormat.YUVJ420P;
    
    // always convert this to yuv420p in filter chains
    public string FFmpegName => FFmpegFormat.YUV420P;
}
