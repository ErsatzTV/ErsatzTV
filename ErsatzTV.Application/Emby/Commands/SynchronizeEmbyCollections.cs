using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyCollections(int EmbyMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IEmbyBackgroundServiceRequest;
