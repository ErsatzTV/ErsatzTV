using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyCollections(string BaseUrl, int EmbyMediaSourceId, bool ForceScan)
    : IRequest<Either<BaseError, Unit>>;
