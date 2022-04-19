using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddSeasonToCollection(int CollectionId, int SeasonId) : IRequest<Either<BaseError, Unit>>;
