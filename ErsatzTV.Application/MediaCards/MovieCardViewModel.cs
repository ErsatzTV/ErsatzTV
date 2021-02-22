namespace ErsatzTV.Application.MediaCards
{
    public record MovieCardViewModel
        (int MovieId, string Title, string Subtitle, string SortTitle, string Poster) : MediaCardViewModel(
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
    }
}
