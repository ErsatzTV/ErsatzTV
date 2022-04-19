using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCards;

public record GetCollectionCards(int Id) : IRequest<Either<BaseError, CollectionCardResultsViewModel>>;
