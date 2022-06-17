namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuv420P10Le : IPixelFormat
{
    public string Name => PixelFormat.YUV420P10LE;
    public string FFmpegName => FFmpegFormat.P010LE;
    public int BitDepth => 10;
}
