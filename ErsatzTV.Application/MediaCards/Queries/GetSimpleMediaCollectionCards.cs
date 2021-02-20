using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetSimpleMediaCollectionCards
        (int Id) : IRequest<Either<BaseError, SimpleMediaCollectionCardResultsViewModel>>;
}
