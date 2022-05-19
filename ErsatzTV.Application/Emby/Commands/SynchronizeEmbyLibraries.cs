using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyLibraries(int EmbyMediaSourceId) : IRequest<Either<BaseError, Unit>>,
    IEmbyBackgroundServiceRequest;
