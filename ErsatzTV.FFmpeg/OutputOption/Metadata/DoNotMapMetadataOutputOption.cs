namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class DoNotMapMetadataOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-map_metadata", "-1" };
}
