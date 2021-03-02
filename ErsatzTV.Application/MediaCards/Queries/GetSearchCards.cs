using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public record GetSearchCards(string Query) : IRequest<Either<BaseError, SearchCardResultsViewModel>>;
}
