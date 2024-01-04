using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinCollections
    (int JellyfinMediaSourceId, bool ForceScan) : IRequest<Either<BaseError, Unit>>;
