﻿using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddItemsToCollection(
    int CollectionId,
    List<int> MovieIds,
    List<int> ShowIds,
    List<int> SeasonIds,
    List<int> EpisodeIds,
    List<int> ArtistIds,
    List<int> MusicVideoIds,
    List<int> OtherVideoIds,
    List<int> SongIds,
    List<int> ImageIds,
    List<int> RemoteStreamIds) : IRequest<Either<BaseError, Unit>>;
