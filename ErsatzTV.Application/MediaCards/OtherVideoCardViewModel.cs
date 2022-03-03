using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record OtherVideoCardViewModel
(
    int OtherVideoId,
    string Title,
    string Subtitle,
    string SortTitle,
    MediaItemState State) : MediaCardViewModel(
    OtherVideoId,
    Title,
    Subtitle,
    SortTitle,
    null,
    State)
{
    public int CustomIndex { get; set; }
}