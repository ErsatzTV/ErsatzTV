using ErsatzTV.Core.Emby;
using MediatR;

namespace ErsatzTV.Application.Emby;

public record GetEmbySecrets : IRequest<EmbySecrets>;