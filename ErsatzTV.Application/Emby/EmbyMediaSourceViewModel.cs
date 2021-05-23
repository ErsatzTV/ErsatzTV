using ErsatzTV.Application.MediaSources;

namespace ErsatzTV.Application.Emby
{
    public record EmbyMediaSourceViewModel(int Id, string Name, string Address) : RemoteMediaSourceViewModel(
        Id,
        Name,
        Address);
}
