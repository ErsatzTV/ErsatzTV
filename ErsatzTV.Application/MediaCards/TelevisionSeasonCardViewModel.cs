namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionSeasonCardViewModel
        (int TelevisionSeasonId, string Title, string Subtitle, string SortTitle, string Poster) : MediaCardViewModel(
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
    }
}
