using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinLibraryById
    (int JellyfinLibraryId, bool ForceScan, bool DeepScan) : IRequest<Either<BaseError, string>>;
