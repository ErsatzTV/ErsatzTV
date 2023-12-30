namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataSubtitleTitleOutputOption : OutputOption
{
    private readonly string _subtitleTitle;

    public MetadataSubtitleTitleOutputOption(string subtitleTitle) => _subtitleTitle = subtitleTitle;

    public override string[] OutputOptions => new[]
        { "-metadata:s:s:0", $"title={_subtitleTitle}" };
}
