namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionEpisodeCardViewModel
    (
        int EpisodeId,
        string Title,
        string Subtitle,
        string SortTitle,
        string Poster,
        string Placeholder) : MediaCardViewModel(
        Title,
        Subtitle,
        SortTitle,
        Poster)
    {
    }
}
