using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record ImageCardViewModel
(
    int ImageId,
    string Title,
    string Subtitle,
    string SortTitle,
    string Poster,
    MediaItemState State) : MediaCardViewModel(
    ImageId,
    Title,
    Subtitle,
    SortTitle,
    Poster,
    State)
{
    public int CustomIndex { get; set; }
}
