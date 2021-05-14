using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Core.Domain;
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
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly ISearchIndex _searchIndex;

        public QuerySearchIndexMoviesHandler(
            ISearchIndex searchIndex,
            IMovieRepository movieRepository,
            IMediaSourceRepository mediaSourceRepository)
        {
            _searchIndex = searchIndex;
            _movieRepository = movieRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<MovieCardResultsViewModel> Handle(
            QuerySearchIndexMovies request,
            CancellationToken cancellationToken)
        {
            SearchResult searchResult = await _searchIndex.Search(
                request.Query,
                (request.PageNumber - 1) * request.PageSize,
                request.PageSize);

            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            List<MovieCardViewModel> items = await _movieRepository
                .GetMoviesForCards(searchResult.Items.Map(i => i.Id).ToList())
                .Map(list => list.Map(m => ProjectToViewModel(m, maybeJellyfin)).ToList());

            return new MovieCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
        }
    }
}
