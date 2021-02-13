namespace ErsatzTV.Application.MediaItems
{
    public record AggregateMediaItemViewModel(
        string Title,
        string Subtitle,
        string SortTitle) : IMediaCard;
}
