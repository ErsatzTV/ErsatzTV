using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Queries
{
    public record GetAllPlexMediaSources : IRequest<List<PlexMediaSourceViewModel>>;
}
