namespace ErsatzTV.Application.MediaItems
{
    public record AggregateMediaItemViewModel(
        int MediaItemId,
        string Title,
        string Subtitle,
        string SortTitle,
        bool HasPoster);
}
