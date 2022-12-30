using ErsatzTV.Core;

namespace ErsatzTV.Scanner.Application.Emby;

public record SynchronizeEmbyCollections(int EmbyMediaSourceId) : IRequest<Either<BaseError, Unit>>;
