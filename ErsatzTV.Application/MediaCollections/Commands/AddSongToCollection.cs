using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddSongToCollection
    (int CollectionId, int SongId) : IRequest<Either<BaseError, Unit>>;
