namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuva420P10Le : IPixelFormat
{
    public string Name => PixelFormat.YUVA420P10LE;
    public string FFmpegName => FFmpegFormat.YUVA420P10;
    public int BitDepth => 10;
}
