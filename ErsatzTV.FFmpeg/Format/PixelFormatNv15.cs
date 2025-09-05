namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatNv15 : IPixelFormat
{
    public PixelFormatNv15(string name) => Name = name;

    public string Name { get; }

    public string FFmpegName => "nv15";
    public int BitDepth => 8;
}
