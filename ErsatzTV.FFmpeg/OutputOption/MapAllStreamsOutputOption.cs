namespace ErsatzTV.FFmpeg.OutputOption;

public class MapAllStreamsOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new[] { "-map", "0" };
}
