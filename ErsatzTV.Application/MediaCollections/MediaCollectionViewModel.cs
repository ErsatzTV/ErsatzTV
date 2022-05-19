using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record MediaCollectionViewModel(
    int Id,
    string Name,
    bool UseCustomPlaybackOrder,
    MediaItemState State) : MediaCardViewModel(
    Id,
    Name,
    string.Empty,
    Name,
    string.Empty,
    State);
