using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record UpdateMultiCollectionItem(int CollectionId, bool ScheduleAsGroup);

    public record UpdateMultiCollection
    (
        int MultiCollectionId,
        string Name,
        List<UpdateMultiCollectionItem> Items) : MediatR.IRequest<Either<BaseError, Unit>>;
}
