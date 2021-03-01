using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Queries
{
    public record GetPlexConnectionParameters
        (int PlexMediaSourceId) : IRequest<Either<BaseError, PlexConnectionParametersViewModel>>;
}
