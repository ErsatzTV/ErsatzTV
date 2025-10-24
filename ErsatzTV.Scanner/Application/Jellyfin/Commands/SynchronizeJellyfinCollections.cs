using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Jellyfin;

public record SynchronizeJellyfinCollections(string BaseUrl, int JellyfinMediaSourceId, bool ForceScan, bool DeepScan)
    : IRequest<Either<BaseError, Unit>>;
