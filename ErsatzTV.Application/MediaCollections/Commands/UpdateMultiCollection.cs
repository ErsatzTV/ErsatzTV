﻿using ErsatzTV.Core;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateMultiCollectionItem(
    int? CollectionId,
    int? SmartCollectionId,
    bool ScheduleAsGroup,
    PlaybackOrder PlaybackOrder);

public record UpdateMultiCollection(
    int MultiCollectionId,
    string Name,
    List<UpdateMultiCollectionItem> Items) : IRequest<Either<BaseError, Unit>>;
