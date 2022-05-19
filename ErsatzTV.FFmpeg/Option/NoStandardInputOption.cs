namespace ErsatzTV.FFmpeg.Option;

public class NoStandardInputOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-nostdin" };
}
