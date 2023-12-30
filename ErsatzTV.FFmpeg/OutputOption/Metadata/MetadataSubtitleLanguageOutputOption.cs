namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataSubtitleLanguageOutputOption : OutputOption
{
    private readonly string _subtitleLanguage;

    public MetadataSubtitleLanguageOutputOption(string subtitleLanguage) => _subtitleLanguage = subtitleLanguage;

    public override string[] OutputOptions => new[]
        { "-metadata:s:s:0", $"language={_subtitleLanguage}" };
}
