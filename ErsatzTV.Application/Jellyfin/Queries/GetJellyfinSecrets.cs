using ErsatzTV.Core.Jellyfin;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinSecrets : IRequest<JellyfinSecrets>;