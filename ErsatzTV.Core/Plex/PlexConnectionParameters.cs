using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.MediaServer;

namespace ErsatzTV.Core.Plex;

public record PlexConnectionParameters
    (PlexConnection Connection, PlexServerAuthToken Token) : MediaServerConnectionParameters;
