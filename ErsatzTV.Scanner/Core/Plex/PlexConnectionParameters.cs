using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Scanner.Core.Plex;

public record PlexConnectionParameters
    (PlexConnection Connection, PlexServerAuthToken Token) : MediaServerConnectionParameters;
