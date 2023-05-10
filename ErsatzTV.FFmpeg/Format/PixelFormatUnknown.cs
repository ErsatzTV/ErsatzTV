namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatUnknown : IPixelFormat
{
    public PixelFormatUnknown(int bitDepth = 8) => BitDepth = bitDepth;

    public string Name => "unknown";
    public string FFmpegName => "unknown";
    public int BitDepth { get; }
}
