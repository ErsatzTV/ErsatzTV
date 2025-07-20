using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Streaming;

public class YamlRemoteStreamDefinition
{
    public string Url { get; set; }
    public string Script { get; set; }
    public string Duration { get; set; }
    [YamlMember(Alias = "fallback_query", ApplyNamingConventions = false)]
    public string FallbackQuery { get; set; }
}
