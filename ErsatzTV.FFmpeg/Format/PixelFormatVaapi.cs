namespace ErsatzTV.FFmpeg.Format;

public class PixelFormatVaapi : IPixelFormat
{
    public PixelFormatVaapi(string name) => Name = name;

    public string Name { get; }

    public string FFmpegName => "vaapi";
    
    public int BitDepth => 8;
}
