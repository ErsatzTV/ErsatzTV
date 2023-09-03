namespace ErsatzTV.FFmpeg.InputOption;

public class NoStandardInputOption : GlobalOption.GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-nostdin" };
}
