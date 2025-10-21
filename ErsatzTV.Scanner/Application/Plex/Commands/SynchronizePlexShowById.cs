using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexShowById(string BaseUrl, int PlexLibraryId, int ShowId, bool DeepScan)
    : IRequest<Either<BaseError, string>>;
