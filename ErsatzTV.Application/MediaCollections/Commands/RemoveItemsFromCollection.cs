using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record RemoveItemsFromCollection(int MediaCollectionId) : IRequest<Either<BaseError, CollectionUpdateResult>>
    {
        public List<int> MediaItemIds { get; set; } = new();
    }
}
