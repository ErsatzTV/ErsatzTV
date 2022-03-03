using ErsatzTV.Application.MediaSources;

namespace ErsatzTV.Application.Jellyfin;

public record JellyfinMediaSourceViewModel(int Id, string Name, string Address) : RemoteMediaSourceViewModel(
    Id,
    Name,
    Address);