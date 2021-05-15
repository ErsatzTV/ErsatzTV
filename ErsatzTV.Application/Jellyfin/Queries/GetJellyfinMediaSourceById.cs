using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetJellyfinMediaSourceById
        (int JellyfinMediaSourceId) : IRequest<Option<JellyfinMediaSourceViewModel>>;
}
