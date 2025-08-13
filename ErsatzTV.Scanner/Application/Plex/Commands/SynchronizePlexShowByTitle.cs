using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexShowByTitle(int PlexLibraryId, string ShowTitle, bool DeepScan)
    : IRequest<Either<BaseError, string>>;