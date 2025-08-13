using YamlDotNet.Serialization;

namespace ErsatzTV.Core.FFmpeg.Selector;

public class StreamSelectorItem
{
    [YamlMember(Alias = "audio_language", ApplyNamingConventions = false)]
    public List<string> AudioLanguages { get; set; } = [];

    [YamlMember(Alias = "audio_title_allowlist", ApplyNamingConventions = false)]
    public List<string> AudioTitleAllowlist { get; set; } = [];

    [YamlMember(Alias = "audio_title_blocklist", ApplyNamingConventions = false)]
    public List<string> AudioTitleBlocklist { get; set; } = [];

    [YamlMember(Alias = "audio_condition", ApplyNamingConventions = false)]
    public string AudioCondition { get; set; }

    [YamlMember(Alias = "disable_subtitles", ApplyNamingConventions = false)]
    public bool DisableSubtitles { get; set; }

    [YamlMember(Alias = "subtitle_language", ApplyNamingConventions = false)]
    public List<string> SubtitleLanguages { get; set; } = [];

    [YamlMember(Alias = "subtitle_title_allowlist", ApplyNamingConventions = false)]
    public List<string> SubtitleTitleAllowlist { get; set; } = [];

    [YamlMember(Alias = "subtitle_title_blocklist", ApplyNamingConventions = false)]
    public List<string> SubtitleTitleBlocklist { get; set; } = [];

    [YamlMember(Alias = "subtitle_condition", ApplyNamingConventions = false)]
    public string SubtitleCondition { get; set; }

    [YamlMember(Alias = "content_condition", ApplyNamingConventions = false)]
    public string ContentCondition { get; set; }
}
