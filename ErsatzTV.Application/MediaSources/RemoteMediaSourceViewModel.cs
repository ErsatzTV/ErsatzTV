namespace ErsatzTV.Application.MediaSources;

public record RemoteMediaSourceViewModel(int Id, string Name, string Address) : MediaSourceViewModel(Id, Name);
