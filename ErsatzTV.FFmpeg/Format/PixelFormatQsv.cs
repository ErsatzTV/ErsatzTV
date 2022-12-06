namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatQsv : IPixelFormat
{
    public PixelFormatQsv(string name) => Name = name;

    public string Name { get; }

    public string FFmpegName => "qsv";
    public int BitDepth => throw new NotSupportedException("This is probably an issue");
}
