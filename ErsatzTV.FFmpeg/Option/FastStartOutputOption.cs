namespace ErsatzTV.FFmpeg.Option;

public class FastStartOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-movflags", "+faststart" };
}
