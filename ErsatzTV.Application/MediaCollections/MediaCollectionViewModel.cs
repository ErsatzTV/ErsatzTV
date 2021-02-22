using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.MediaCollections
{
    public record MediaCollectionViewModel(int Id, string Name) : MediaCardViewModel(
        Name,
        string.Empty,
        Name,
        string.Empty);
}
