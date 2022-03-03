namespace ErsatzTV.Application.Emby;

public record GetAllEmbyMediaSources : IRequest<List<EmbyMediaSourceViewModel>>;