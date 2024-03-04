namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatVaapi : IPixelFormat
{
    public PixelFormatVaapi(string name, int bitDepth = 8)
    {
        Name = name;
        BitDepth = bitDepth;
    }

    public string Name { get; }

    public string FFmpegName => "vaapi";

    public int BitDepth { get; }
}
