using Newtonsoft.Json;

namespace ErsatzTV.Core.Streaming;

public class ExternalJsonChannel
{
    [JsonProperty("startTime")]
    public string StartTime { get; set; }
    
    [JsonProperty("guideMinimumDurationSeconds")]
    public int GuideMinimumDurationSeconds { get; set; }

    [JsonProperty("programs")]
    public ExternalJsonProgram[] Programs { get; set; }
}