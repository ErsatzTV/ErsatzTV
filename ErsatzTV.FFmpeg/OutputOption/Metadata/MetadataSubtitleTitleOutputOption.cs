namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataSubtitleTitleOutputOption : OutputOption
{
    private readonly string _subtitleTitle;

    public MetadataSubtitleTitleOutputOption(string subtitleTitle) => _subtitleTitle = subtitleTitle;

    public override IList<string> OutputOptions => new List<string>
        { "-metadata:s:s:0", $"title={_subtitleTitle}" };
}
