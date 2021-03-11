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
            request.Query.Split(":").Head() switch
            {
                "genre" => GenreSearch(request.Query.Replace("genre:", string.Empty)),
                "tag" => TagSearch(request.Query.Replace("tag:", string.Empty)),
                _ => TitleSearch(request.Query)
            };

        private Task<Either<BaseError, SearchCardResultsViewModel>> TitleSearch(string query) =>
            Try(_searchRepository.SearchMediaItemsByTitle(query)).Sequence()
                .Map(ProjectToSearchResults)
                .Map(t => t.ToEither(ex => BaseError.New($"Failed to search: {ex.Message}")));

        private Task<Either<BaseError, SearchCardResultsViewModel>> GenreSearch(string query) =>
            Try(_searchRepository.SearchMediaItemsByGenre(query)).Sequence()
                .Map(ProjectToSearchResults)
                .Map(t => t.ToEither(ex => BaseError.New($"Failed to search: {ex.Message}")));

        private Task<Either<BaseError, SearchCardResultsViewModel>> TagSearch(string query) =>
            Try(_searchRepository.SearchMediaItemsByTag(query)).Sequence()
                .Map(ProjectToSearchResults)
                .Map(t => t.ToEither(ex => BaseError.New($"Failed to search: {ex.Message}")));
    }
}
