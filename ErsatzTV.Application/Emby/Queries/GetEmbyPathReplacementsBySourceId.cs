namespace ErsatzTV.Application.Emby;

public record GetEmbyPathReplacementsBySourceId
    (int EmbyMediaSourceId) : IRequest<List<EmbyPathReplacementViewModel>>;