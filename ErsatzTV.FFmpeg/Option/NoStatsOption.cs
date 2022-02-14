namespace ErsatzTV.FFmpeg.Option;

public class NoStatsOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-nostats" };
}
