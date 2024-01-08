using Newtonsoft.Json;

namespace ErsatzTV.Core.Streaming;

public class ExternalJsonProgram
{
    [JsonProperty("title")]
    public string Title { get; set; }
    
    [JsonProperty("showTitle")]
    public string ShowTitle { get; set; }
    
    [JsonProperty("season")]
    public int Season { get; set; }
    
    [JsonProperty("episode")]
    public int Episode { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }
    
    [JsonProperty("duration")]
    public int Duration { get; set; }
    
    [JsonProperty("plexFile")]
    public string PlexFile { get; set; }
    
    [JsonProperty("file")]
    public string File { get; set; }
    
    [JsonProperty("serverKey")]
    public string ServerKey { get; set; }
}
