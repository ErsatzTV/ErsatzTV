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
        TelevisionSeasonId,
        Title,
        Subtitle,
        SortTitle,
        Poster)
    {
    }
}
