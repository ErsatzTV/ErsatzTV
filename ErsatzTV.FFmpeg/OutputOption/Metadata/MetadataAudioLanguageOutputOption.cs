namespace ErsatzTV.FFmpeg.OutputOption.Metadata;

public class MetadataAudioLanguageOutputOption : OutputOption
{
    private readonly string _audioLanguage;

    public MetadataAudioLanguageOutputOption(string audioLanguage) => _audioLanguage = audioLanguage;

    public override string[] OutputOptions => new[]
        { "-metadata:s:a:0", $"language={_audioLanguage}" };
}
