using ErsatzTV.Core.Domain.MediaServer;

namespace ErsatzTV.Core.Jellyfin;

public record JellyfinConnectionParameters(string Address, string ApiKey, int MediaSourceId)
    : MediaServerConnectionParameters
{
    public string AuthorizationHeader => $"MediaBrowser Token={ApiKey}";
}
