using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Plex.Queries
{
    public record GetPlexMediaSourceById(int PlexMediaSourceId) : IRequest<Option<PlexMediaSourceViewModel>>;
}
