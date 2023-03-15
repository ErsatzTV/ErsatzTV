using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyCollections(int EmbyMediaSourceId, bool ForceScan) : IRequest<Either<BaseError, Unit>>;
