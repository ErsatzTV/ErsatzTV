using ErsatzTV.Core.Jellyfin;
using MediatR;

namespace ErsatzTV.Application.Jellyfin.Queries
{
    public record GetJellyfinSecrets : IRequest<JellyfinSecrets>;
}
