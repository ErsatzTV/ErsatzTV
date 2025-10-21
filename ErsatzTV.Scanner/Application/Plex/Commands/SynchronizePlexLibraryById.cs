using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexLibraryById(string BaseUrl, int PlexLibraryId, bool ForceScan, bool DeepScan)
    : IRequest<Either<BaseError, string>>;
