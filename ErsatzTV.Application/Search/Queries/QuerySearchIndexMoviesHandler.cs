using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Search;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search.Queries
{
    public class QuerySearchIndexMoviesHandler : IRequestHandler<QuerySearchIndexMovies, MovieCardResultsViewModel>
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ISearchIndex _searchIndex;

        public QuerySearchIndexMoviesHandler(ISearchIndex searchIndex, IMovieRepository movieRepository)
        {
            _searchIndex = searchIndex;
            _movieRepository = movieRepository;
        }

        public async Task<MovieCardResultsViewModel> Handle(
            QuerySearchIndexMovies request,
            CancellationToken cancellationToken)
        {
            (List<SearchItem> searchItems, int totalCount) =
                await _searchIndex.Search(
                    request.Query,
                    (request.PageNumber - 1) * request.PageSize,
                    request.PageSize);

            List<MovieCardViewModel> items = await _movieRepository
                .GetMoviesForCards(searchItems.Map(i => i.Id).ToList())
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new MovieCardResultsViewModel(totalCount, items);
        }
    }
}
