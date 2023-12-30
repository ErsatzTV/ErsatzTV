namespace ErsatzTV.FFmpeg.OutputOption;

public class MapAllStreamsOutputOption : OutputOption
{
    public override string[] OutputOptions => new[] { "-map", "0" };
}
