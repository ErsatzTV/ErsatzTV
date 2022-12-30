using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexLibraryById
    (int PlexLibraryId, bool ForceScan, bool DeepScan) : IRequest<Either<BaseError, string>>;
