using System.Collections.Generic;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record CreateMultiCollectionItem(int CollectionId, bool ScheduleAsGroup);

    public record CreateMultiCollection
        (string Name, List<CreateMultiCollectionItem> Items) : IRequest<Either<BaseError, MultiCollectionViewModel>>;
}
