using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinShowByTitle(int JellyfinLibraryId, string ShowTitle, bool DeepScan)
    : IRequest<Either<BaseError, string>>;