using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record RemoveItemsFromCollection(int MediaCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>
    {
        public List<int> MediaItemIds { get; set; } = new();
    }
}
