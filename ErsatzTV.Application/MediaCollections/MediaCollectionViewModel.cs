using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.MediaCollections
{
    public record MediaCollectionViewModel(int Id, string Name, bool UseCustomPlaybackOrder) : MediaCardViewModel(
        Id,
        Name,
        string.Empty,
        Name,
        string.Empty);
}
