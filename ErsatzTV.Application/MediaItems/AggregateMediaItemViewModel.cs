namespace ErsatzTV.Application.MediaItems
{
    public record AggregateMediaItemViewModel(
        string Source,
        string Title,
        string Subtitle,
        int Count,
        string Duration) : IMediaCard
    {
        public string SortTitle =>
            Title.ToLowerInvariant().StartsWith("the ") ? Title.Substring(4) : Title;
    }
}
