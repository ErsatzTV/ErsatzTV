namespace ErsatzTV.FFmpeg.Option.Metadata;

public class MetadataAudioLanguageOutputOption : OutputOption
{
    private readonly string _audioLanguage;

    public MetadataAudioLanguageOutputOption(string audioLanguage)
    {
        _audioLanguage = audioLanguage;
    }

    public override IList<string> OutputOptions => new List<string>
        { "-metadata:s:a:0", $"language={_audioLanguage}" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        MetadataAudioLanguage = _audioLanguage
    };
}
