namespace ErsatzTV.FFmpeg.OutputOption;

public class FastStartOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-movflags", "+faststart" };
}
