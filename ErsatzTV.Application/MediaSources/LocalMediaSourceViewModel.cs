namespace ErsatzTV.Application.MediaSources
{
    public record LocalMediaSourceViewModel(int Id, string Name, string Folder)
        : MediaSourceViewModel(Id, Name);
}
