namespace ErsatzTV.FFmpeg.GlobalOption;

public class ProgressOption : GlobalOption
{
    public override string[] GlobalOptions => ["-progress", "-"];
}
