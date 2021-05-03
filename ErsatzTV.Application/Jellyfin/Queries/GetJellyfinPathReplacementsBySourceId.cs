using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetJellyfinPathReplacementsBySourceId
        (int JellyfinMediaSourceId) : IRequest<List<JellyfinPathReplacementViewModel>>;
}
