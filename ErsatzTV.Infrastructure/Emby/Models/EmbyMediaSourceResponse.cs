namespace ErsatzTV.Infrastructure.Emby.Models;

public class EmbyMediaSourceResponse
{
    public string Id { get; set; }
    public string Protocol { get; set; }
    public long RunTimeTicks { get; set; }
    public IList<EmbyMediaStreamResponse> MediaStreams { get; set; }
}
