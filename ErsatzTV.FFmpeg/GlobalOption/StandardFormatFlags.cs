namespace ErsatzTV.FFmpeg.GlobalOption;

public class StandardFormatFlags : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-fflags", "+genpts+discardcorrupt+igndts" };
}
