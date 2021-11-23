namespace ErsatzTV.Application.MediaCards
{
    public record SongCardViewModel
    (
        int SongId,
        string Title,
        string Subtitle,
        string SortTitle) : MediaCardViewModel(
        SongId,
        Title,
        Subtitle,
        SortTitle,
        null)
    {
        public int CustomIndex { get; set; }
    }
}
