using ErsatzTV.Core;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Application.Plex;

public record TryCompletePlexPinFlow(PlexAuthPin AuthPin) : IRequest<Either<BaseError, bool>>,
    IPlexBackgroundServiceRequest;