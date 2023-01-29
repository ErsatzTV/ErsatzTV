namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatP010 : IPixelFormat
{
    public string Name => "p010le";
    public string FFmpegName => "p010";
    public int BitDepth => 10;
}
