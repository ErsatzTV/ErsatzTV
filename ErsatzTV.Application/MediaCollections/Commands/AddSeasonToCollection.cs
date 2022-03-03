using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record AddSeasonToCollection(int CollectionId, int SeasonId) : MediatR.IRequest<Either<BaseError, Unit>>;