namespace ErsatzTV.Application.MediaCards
{
    public record ArtistCardViewModel
        (int ArtistId, string Title, string Subtitle, string SortTitle, string Poster) : MediaCardViewModel(
            ArtistId,
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
    }
}
