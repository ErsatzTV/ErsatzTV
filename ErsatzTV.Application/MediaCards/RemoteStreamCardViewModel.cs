using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record RemoteStreamCardViewModel(
    int RemoteStreamId,
    string Title,
    string Subtitle,
    string SortTitle,
    string Poster,
    MediaItemState State) : MediaCardViewModel(
    RemoteStreamId,
    Title,
    Subtitle,
    SortTitle,
    Poster,
    State,
    HasMediaInfo: true)
{
    public int CustomIndex { get; set; }
}
