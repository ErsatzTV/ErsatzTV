namespace ErsatzTV.Application.Jellyfin;

public record GetAllJellyfinMediaSources : IRequest<List<JellyfinMediaSourceViewModel>>;
