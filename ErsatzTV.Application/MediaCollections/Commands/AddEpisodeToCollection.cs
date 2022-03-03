using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddEpisodeToCollection(int CollectionId, int EpisodeId) : MediatR.IRequest<Either<BaseError, Unit>>;