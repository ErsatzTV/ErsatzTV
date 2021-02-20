namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionSeasonCardViewModel
    (
        int TelevisionSeasonId,
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
