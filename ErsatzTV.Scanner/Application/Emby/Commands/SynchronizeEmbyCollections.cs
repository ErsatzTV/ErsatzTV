using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyCollections(string BaseUrl, int EmbyMediaSourceId, bool ForceScan, bool DeepScan)
    : IRequest<Either<BaseError, Unit>>;
