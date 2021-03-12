namespace ErsatzTV.Application.MediaCards
{
    public record MovieCardViewModel
        (int MovieId, string Title, string Subtitle, string SortTitle, string Poster) : MediaCardViewModel(
            MovieId,
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
        public int CustomIndex { get; set; }
    }
}
