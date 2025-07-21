using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record MusicVideoCardViewModel(
    int MusicVideoId,
    string Title,
    string Subtitle,
    string SortTitle,
    string Plot,
    string Album,
    string Poster,
    MediaItemState State,
    string Path,
    string LocalPath) : MediaCardViewModel(
    MusicVideoId,
    Title,
    Subtitle,
    SortTitle,
    Poster,
    State,
    HasMediaInfo: true)
{
    public int CustomIndex { get; set; }
}
