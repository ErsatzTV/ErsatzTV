using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class GetSearchCardsHandler : IRequestHandler<GetSearchCards, Either<BaseError, SearchCardResultsViewModel>>
    {
        private readonly ISearchRepository _searchRepository;

        public GetSearchCardsHandler(ISearchRepository searchRepository) => _searchRepository = searchRepository;

        public Task<Either<BaseError, SearchCardResultsViewModel>> Handle(
            GetSearchCards request,
            CancellationToken cancellationToken) =>
            Try(_searchRepository.SearchMediaItems(request.Query)).Sequence()
                .Map(ProjectToSearchResults)
                .Map(t => t.ToEither(ex => BaseError.New($"Failed to search: {ex.Message}")));
    }
}
