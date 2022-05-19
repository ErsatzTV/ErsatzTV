using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record RemoveItemsFromCollection(int MediaCollectionId) : IRequest<Either<BaseError, Unit>>
{
    public List<int> MediaItemIds { get; set; } = new();
}
