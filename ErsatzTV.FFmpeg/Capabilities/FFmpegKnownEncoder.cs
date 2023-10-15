namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownEncoder
{
    public string Name { get; }

    private FFmpegKnownEncoder(string Name)
    {
        this.Name = Name;
    }

    public static IList<string> AllEncoders => Array.Empty<string>();
}
