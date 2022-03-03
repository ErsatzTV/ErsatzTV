namespace ErsatzTV.Application.MediaSources;

public record LocalMediaSourceViewModel(int Id) : MediaSourceViewModel(Id, "Local");