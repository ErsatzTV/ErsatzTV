using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCards;

public record GetCollectionCards(int Id) : IRequest<Either<BaseError, CollectionCardResultsViewModel>>;