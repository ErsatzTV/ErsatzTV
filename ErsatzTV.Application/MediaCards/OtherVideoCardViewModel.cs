namespace ErsatzTV.Application.MediaCards
{
    public record OtherVideoCardViewModel
    (
        int OtherVideoId,
        string Title,
        string Subtitle,
        string SortTitle) : MediaCardViewModel(
        OtherVideoId,
        Title,
        Subtitle,
        SortTitle,
        null)
    {
        public int CustomIndex { get; set; }
    }
}
