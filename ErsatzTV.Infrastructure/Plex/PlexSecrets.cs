using System.Collections.Generic;

namespace ErsatzTV.Infrastructure.Plex;

public class PlexSecrets
{
    public string ClientIdentifier { get; set; }
    public Dictionary<string, string> UserAuthTokens { get; set; }
    public Dictionary<string, string> ServerAuthTokens { get; set; }
}