namespace ErsatzTV.FFmpeg.OutputOption;

public class NoAutoScaleOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-noautoscale" };
}
