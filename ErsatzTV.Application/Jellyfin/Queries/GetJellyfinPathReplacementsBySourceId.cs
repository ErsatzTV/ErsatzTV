namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinPathReplacementsBySourceId
    (int JellyfinMediaSourceId) : IRequest<List<JellyfinPathReplacementViewModel>>;
