namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownFilter
{
    public static readonly FFmpegKnownFilter ScaleNpp = new("scale_npp");

    private FFmpegKnownFilter(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllFilters =>
        new[]
        {
            ScaleNpp.Name
        };
}
