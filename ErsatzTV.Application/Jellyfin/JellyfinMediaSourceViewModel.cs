using ErsatzTV.Application.MediaSources;

namespace ErsatzTV.Application.Jellyfin
{
    public record JellyfinMediaSourceViewModel(int Id, string Name, string Address) : MediaSourceViewModel(Id, Name);
}
