using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record UpdateCollectionCustomOrder
    (
        int CollectionId,
        List<MediaItemCustomOrder> MediaItemCustomOrders) : MediatR.IRequest<Either<BaseError, Unit>>;

    public record MediaItemCustomOrder(int MediaItemId, int CustomIndex);
}
