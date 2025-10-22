using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexNetworks(string BaseUrl, int PlexLibraryId, bool ForceScan)
    : IRequest<Either<BaseError, Unit>>;
