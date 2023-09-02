namespace ErsatzTV.FFmpeg.OutputOption;

public class ClosedGopOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-flags", "cgop" };
}
