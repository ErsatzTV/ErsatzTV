namespace ErsatzTV.FFmpeg.OutputOption;

public class ClosedGopOutputOption : OutputOption
{
    public override string[] OutputOptions => new[] { "-flags", "cgop" };
}
