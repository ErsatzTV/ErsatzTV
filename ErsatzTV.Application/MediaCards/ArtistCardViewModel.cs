using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    public record ArtistCardViewModel
    (
        int ArtistId,
        string Title,
        string Subtitle,
        string SortTitle,
        string Poster,
        MediaItemState State) : MediaCardViewModel(
        ArtistId,
        Title,
        Subtitle,
        SortTitle,
        Poster,
        State);
}
