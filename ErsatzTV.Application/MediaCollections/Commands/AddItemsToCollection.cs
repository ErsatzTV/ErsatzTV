using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddItemsToCollection
        (int CollectionId, List<int> MovieIds, List<int> ShowIds, List<int> MusicVideoIds) : MediatR.IRequest<Either<BaseError, Unit>>;
}
