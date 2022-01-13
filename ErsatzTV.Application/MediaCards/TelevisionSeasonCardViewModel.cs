using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionSeasonCardViewModel
    (
        string ShowTitle,
        int TelevisionSeasonId,
        int TelevisionSeasonNumber,
        string Title,
        string Subtitle,
        string SortTitle,
        string Poster,
        string Placeholder,
        MediaItemState State) : MediaCardViewModel(
        TelevisionSeasonId,
        Title,
        Subtitle,
        SortTitle,
        Poster,
        State);
}
