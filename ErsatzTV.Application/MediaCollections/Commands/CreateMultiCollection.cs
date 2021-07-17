using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record CreateMultiCollectionItem(int CollectionId, bool ScheduleAsGroup, PlaybackOrder PlaybackOrder);

    public record CreateMultiCollection
        (string Name, List<CreateMultiCollectionItem> Items) : IRequest<Either<BaseError, MultiCollectionViewModel>>;
}
