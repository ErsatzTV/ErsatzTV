using ErsatzTV.Core;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyLibraries(int EmbyMediaSourceId) : MediatR.IRequest<Either<BaseError, Unit>>,
    IEmbyBackgroundServiceRequest;