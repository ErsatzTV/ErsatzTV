namespace ErsatzTV.Infrastructure.Jellyfin.Models;

public class JellyfinMediaSourceResponse
{
    public long RunTimeTicks { get; set; }
    public IList<JellyfinMediaStreamResponse> MediaStreams { get; set; }
}
