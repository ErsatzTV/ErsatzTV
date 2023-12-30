namespace ErsatzTV.FFmpeg.GlobalOption;

public class StandardFormatFlags : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-fflags", "+genpts+discardcorrupt+igndts" };
}
