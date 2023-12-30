namespace ErsatzTV.FFmpeg.GlobalOption;

public class HideBannerOption : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-hide_banner" };
}
