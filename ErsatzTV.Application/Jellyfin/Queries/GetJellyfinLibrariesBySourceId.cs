namespace ErsatzTV.Application.Jellyfin;

public record GetJellyfinLibrariesBySourceId(int JellyfinMediaSourceId) : IRequest<List<JellyfinLibraryViewModel>>;