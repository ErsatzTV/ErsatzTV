namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionEpisodeCardViewModel
        (string Title, string Subtitle, string SortTitle, string Poster) : MediaCardViewModel(
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
    }
}
