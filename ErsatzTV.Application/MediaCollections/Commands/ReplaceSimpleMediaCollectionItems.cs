using System.Collections.Generic;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record ReplaceSimpleMediaCollectionItems
        (int MediaCollectionId, List<int> MediaItemIds) : IRequest<Either<BaseError, List<MediaItemViewModel>>>;
}
