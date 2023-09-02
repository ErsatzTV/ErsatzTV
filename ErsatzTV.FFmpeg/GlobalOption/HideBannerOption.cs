namespace ErsatzTV.FFmpeg.GlobalOption;

public class HideBannerOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hide_banner" };
}
