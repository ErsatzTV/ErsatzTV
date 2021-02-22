namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionSeasonCardViewModel
    (
        string ShowTitle,
        int TelevisionSeasonId,
        int TelevisionSeasonNumber,
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
