using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public record GetAllEmbyMediaSources : IRequest<List<EmbyMediaSourceViewModel>>;
}
