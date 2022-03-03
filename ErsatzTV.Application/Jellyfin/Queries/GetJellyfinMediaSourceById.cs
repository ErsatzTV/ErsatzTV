using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinMediaSourceById
    (int JellyfinMediaSourceId) : IRequest<Option<JellyfinMediaSourceViewModel>>;