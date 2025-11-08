namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownFilter
{
    public static readonly FFmpegKnownFilter ScaleNpp = new("scale_npp");
    public static readonly FFmpegKnownFilter TonemapOpenCL = new("tonemap_opencl");
    public static readonly FFmpegKnownFilter Libplacebo = new("libplacebo");

    private FFmpegKnownFilter(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllFilters =>
    [
        ScaleNpp.Name,
        TonemapOpenCL.Name
    ];
}
