namespace ErsatzTV.Application.MediaCards
{
    public record MusicVideoCardViewModel
        (int MusicVideoId, string Title, string Subtitle, string SortTitle, string Plot, string Poster) : MediaCardViewModel(
            MusicVideoId,
            Title,
            Subtitle,
            SortTitle,
            Poster)
    {
        public int CustomIndex { get; set; }
    }
}
