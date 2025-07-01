namespace ErsatzTV.FFmpeg.OutputOption;

public class ColorMetadataOutputOption : OutputOption
{
    public override string[] OutputOptions =>
    [
        "-color_primaries", "bt709",
        "-color_trc", "bt709",
        "-colorspace", "bt709",
        "-color_range", "tv"
    ];
}
