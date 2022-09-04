namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatYuva420P : IPixelFormat
{
    public string Name => PixelFormat.YUVA420P;
    public string FFmpegName => FFmpegFormat.YUVA420P;
    public int BitDepth => 8;
}
