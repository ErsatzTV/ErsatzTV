namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatCuda(string name, int bitDepth = 8) : IPixelFormat
{
    public string Name { get; } = name;

    public string FFmpegName => "cuda";

    public int BitDepth { get; } = bitDepth;
}
