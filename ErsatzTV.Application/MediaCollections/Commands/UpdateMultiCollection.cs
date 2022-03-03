using System.Collections.Generic;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateMultiCollectionItem(int? CollectionId, int? SmartCollectionId, bool ScheduleAsGroup, PlaybackOrder PlaybackOrder);

public record UpdateMultiCollection
(
    int MultiCollectionId,
    string Name,
    List<UpdateMultiCollectionItem> Items) : MediatR.IRequest<Either<BaseError, Unit>>;