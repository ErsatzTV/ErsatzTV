using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionShowCardViewModel
    (
        int TelevisionShowId,
        string Title,
        string Subtitle,
        string SortTitle,
        string Poster,
        MediaItemState State) : MediaCardViewModel(
        TelevisionShowId,
        Title,
        Subtitle,
        SortTitle,
        Poster,
        State);
}
