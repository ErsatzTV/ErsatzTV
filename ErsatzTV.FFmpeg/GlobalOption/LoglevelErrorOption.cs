namespace ErsatzTV.FFmpeg.GlobalOption;

public class LoglevelErrorOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-loglevel", "error" };
}
