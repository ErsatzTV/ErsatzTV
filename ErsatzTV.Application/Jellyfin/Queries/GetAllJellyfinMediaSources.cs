using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetAllJellyfinMediaSources : IRequest<List<JellyfinMediaSourceViewModel>>;
}
