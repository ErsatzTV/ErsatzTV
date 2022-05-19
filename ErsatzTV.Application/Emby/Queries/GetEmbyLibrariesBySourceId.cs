namespace ErsatzTV.Application.Emby;

public record GetEmbyLibrariesBySourceId(int EmbyMediaSourceId) : IRequest<List<EmbyLibraryViewModel>>;
