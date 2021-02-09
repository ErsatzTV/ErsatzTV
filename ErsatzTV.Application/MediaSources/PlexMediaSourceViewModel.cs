using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaSources
{
    public record PlexMediaSourceViewModel(int Id, string Name, string Address) : MediaSourceViewModel(
        Id,
        Name,
        MediaSourceType.Plex);
}
