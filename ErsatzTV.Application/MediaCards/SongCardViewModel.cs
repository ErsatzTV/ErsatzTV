using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    public record SongCardViewModel
    (
        int SongId,
        string Title,
        string Subtitle,
        string SortTitle,
        MediaItemState State) : MediaCardViewModel(
        SongId,
        Title,
        Subtitle,
        SortTitle,
        null,
        State)
    {
        public int CustomIndex { get; set; }
    }
}
