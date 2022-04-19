namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatNv12 : IPixelFormat
{
    public PixelFormatNv12(string name) => Name = name;

    public string Name { get; }

    public string FFmpegName => "nv12";
}
