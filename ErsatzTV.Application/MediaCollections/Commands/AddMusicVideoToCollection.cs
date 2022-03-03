using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddMusicVideoToCollection
    (int CollectionId, int MusicVideoId) : MediatR.IRequest<Either<BaseError, Unit>>;