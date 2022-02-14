namespace ErsatzTV.FFmpeg.Option;

public class ClosedGopOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-flags", "cgop" };
}
