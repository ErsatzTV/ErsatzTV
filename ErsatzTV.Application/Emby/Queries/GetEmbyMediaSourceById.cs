namespace ErsatzTV.Application.Emby;

public record GetEmbyMediaSourceById(int EmbyMediaSourceId) : IRequest<Option<EmbyMediaSourceViewModel>>;