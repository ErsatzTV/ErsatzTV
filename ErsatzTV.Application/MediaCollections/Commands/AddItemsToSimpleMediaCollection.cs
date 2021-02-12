using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddItemsToSimpleMediaCollection
        (int MediaCollectionId, List<int> ItemIds) : MediatR.IRequest<Either<BaseError, Unit>>;
}
