using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddEpisodeToCollection(int CollectionId, int EpisodeId) : IRequest<Either<BaseError, Unit>>;
