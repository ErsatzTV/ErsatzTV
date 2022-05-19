namespace ErsatzTV.Application.Plex;

public record GetAllPlexMediaSources : IRequest<List<PlexMediaSourceViewModel>>;
