using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public record GetEmbyLibrariesBySourceId(int EmbyMediaSourceId) : IRequest<List<EmbyLibraryViewModel>>;
}
