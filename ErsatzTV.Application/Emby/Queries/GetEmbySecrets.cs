using ErsatzTV.Core.Emby;
using MediatR;

namespace ErsatzTV.Application.Emby.Queries
{
    public record GetEmbySecrets : IRequest<EmbySecrets>;
}
