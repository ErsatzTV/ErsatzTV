using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Emby;

public record SynchronizeEmbyMediaSources : IRequest<Either<BaseError, List<EmbyMediaSource>>>,
    IEmbyBackgroundServiceRequest;
