using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record CreateMultiCollectionItem(int? CollectionId, int? SmartCollectionId, bool ScheduleAsGroup, PlaybackOrder PlaybackOrder);

public record CreateMultiCollection
    (string Name, List<CreateMultiCollectionItem> Items) : IRequest<Either<BaseError, MultiCollectionViewModel>>;