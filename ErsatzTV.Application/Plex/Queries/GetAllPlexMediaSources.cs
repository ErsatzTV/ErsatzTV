using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Plex.Queries
{
    public record GetAllPlexMediaSources : IRequest<List<PlexMediaSourceViewModel>>;
}
