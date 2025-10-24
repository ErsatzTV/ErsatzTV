using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Plex;

public record SynchronizePlexCollections(string BaseUrl, int PlexMediaSourceId, bool ForceScan, bool DeepScan)
    : IRequest<Either<BaseError, Unit>>;
