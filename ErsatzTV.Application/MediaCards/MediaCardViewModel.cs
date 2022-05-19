using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCards;

public record MediaCardViewModel(
    int MediaItemId,
    string Title,
    string Subtitle,
    string SortTitle,
    string Poster,
    MediaItemState State);
