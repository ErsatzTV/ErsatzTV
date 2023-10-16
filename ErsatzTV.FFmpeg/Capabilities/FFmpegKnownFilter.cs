namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownFilter
{
    public string Name { get; }

    private FFmpegKnownFilter(string Name)
    {
        this.Name = Name;
    }

    public static readonly FFmpegKnownFilter ScaleNpp = new("scale_npp");

    public static IList<string> AllFilters =>
        new[]
        {
            ScaleNpp.Name
        };
}
