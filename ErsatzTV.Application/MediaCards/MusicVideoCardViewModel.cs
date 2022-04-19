using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record MusicVideoCardViewModel
(
    int MusicVideoId,
    string Title,
    string Subtitle,
    string SortTitle,
    string Plot,
    string Album,
    string Poster,
    MediaItemState State,
    string Path) : MediaCardViewModel(
    MusicVideoId,
    Title,
    Subtitle,
    SortTitle,
    Poster,
    State)
{
    public int CustomIndex { get; set; }
}
