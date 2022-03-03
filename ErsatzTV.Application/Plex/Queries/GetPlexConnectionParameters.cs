using ErsatzTV.Core;

namespace ErsatzTV.Application.Plex;

public record GetPlexConnectionParameters
    (int PlexMediaSourceId) : IRequest<Either<BaseError, PlexConnectionParametersViewModel>>;