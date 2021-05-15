using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetJellyfinLibrariesBySourceId(int JellyfinMediaSourceId) : IRequest<List<JellyfinLibraryViewModel>>;
}
