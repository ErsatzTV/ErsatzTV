namespace ErsatzTV.FFmpeg.InputOption;

public class NoStandardInputOption : GlobalOption.GlobalOption
{
    public override string[] GlobalOptions => new[] { "-nostdin" };
}
