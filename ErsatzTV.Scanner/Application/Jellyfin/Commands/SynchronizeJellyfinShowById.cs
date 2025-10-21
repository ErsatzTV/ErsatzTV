using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinShowById(string BaseUrl, int JellyfinLibraryId, int ShowId, bool DeepScan)
    : IRequest<Either<BaseError, string>>;
