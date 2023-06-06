namespace ErsatzTV.FFmpeg.Option.Metadata;

public class MetadataSubtitleLanguageOutputOption : OutputOption
{
    private readonly string _subtitleLanguage;

    public MetadataSubtitleLanguageOutputOption(string subtitleLanguage) => _subtitleLanguage = subtitleLanguage;

    public override IList<string> OutputOptions => new List<string>
        { "-metadata:s:s:0", $"language={_subtitleLanguage}" };
}
