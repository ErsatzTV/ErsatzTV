using Newtonsoft.Json;

namespace ErsatzTV.Core.Streaming;

public class ExternalJsonChannel
{
    [JsonProperty("startTime")]
    public string StartTime { get; set; }

    public ExternalJsonProgram[] Programs { get; set; }
}