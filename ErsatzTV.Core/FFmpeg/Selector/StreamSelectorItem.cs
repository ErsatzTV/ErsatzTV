using YamlDotNet.Serialization;

namespace ErsatzTV.Core.FFmpeg.Selector;

public class StreamSelectorItem
{
    [YamlMember(Alias = "audio_language", ApplyNamingConventions = false)]
    public List<string> AudioLanguages { get; set; } = [];

    [YamlMember(Alias = "audio_metadata", ApplyNamingConventions = false)]
    public StreamMetadata AudioMetadata { get; set; } = StreamMetadata.None;

    [YamlMember(Alias = "disable_subtitles", ApplyNamingConventions = false)]
    public bool DisableSubtitles { get; set; }

    [YamlMember(Alias = "subtitle_language", ApplyNamingConventions = false)]
    public List<string> SubtitleLanguages { get; set; } = [];

    [YamlMember(Alias = "subtitle_metadata", ApplyNamingConventions = false)]
    public StreamMetadata SubtitleMetadata { get; set; } = StreamMetadata.None;
}
