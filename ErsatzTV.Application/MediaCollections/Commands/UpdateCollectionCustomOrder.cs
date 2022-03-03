using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateCollectionCustomOrder
(
    int CollectionId,
    List<MediaItemCustomOrder> MediaItemCustomOrders) : MediatR.IRequest<Either<BaseError, Unit>>;

public record MediaItemCustomOrder(int MediaItemId, int CustomIndex);