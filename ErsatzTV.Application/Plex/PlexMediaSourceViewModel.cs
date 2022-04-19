using ErsatzTV.Application.MediaSources;

namespace ErsatzTV.Application.Plex;

public record PlexMediaSourceViewModel(int Id, string Name, string Address) : MediaSourceViewModel(Id, Name);
