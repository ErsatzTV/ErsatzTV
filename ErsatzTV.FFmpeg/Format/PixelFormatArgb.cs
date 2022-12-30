namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatArgb : IPixelFormat
{
    public string Name => PixelFormat.ARGB;
    public string FFmpegName => FFmpegFormat.ARGB;
    public int BitDepth => 8;
}
