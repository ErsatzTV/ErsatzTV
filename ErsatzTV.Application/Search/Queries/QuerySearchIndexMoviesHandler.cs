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
using LanguageExt.UnsafeValueAccess;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.Search.Queries
{
    public class QuerySearchIndexMoviesHandler : IRequestHandler<QuerySearchIndexMovies, MovieCardResultsViewModel>
    {
        private readonly IMovieRepository _movieRepository;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly ISearchIndex _searchIndex;

        public QuerySearchIndexMoviesHandler(ISearchIndex searchIndex, IMovieRepository movieRepository, IMediaSourceRepository mediaSourceRepository)
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

            List<MovieCardViewModel> items = await _movieRepository
                .GetMoviesForCards(searchResult.Items.Map(i => i.Id).ToList())
                .Map(list => list.Map(ProjectToViewModel).ToList());
            
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            if (maybeJellyfin.IsSome)
            {
                var newItems = new List<MovieCardViewModel>();
                JellyfinMediaSource jellyfin = maybeJellyfin.ValueUnsafe();

                foreach (MovieCardViewModel item in items)
                {
                    if (item.Poster.StartsWith("jellyfin://"))
                    {
                        string poster = item.Poster.Replace("jellyfin://", jellyfin.Connections.Head().Address) +
                                        "&fillHeight=220";
                        newItems.Add(item with { Poster = poster });
                    }
                    else
                    {
                        newItems.Add(item);
                    }
                }

                items = newItems;
            }

            return new MovieCardResultsViewModel(searchResult.TotalCount, items, searchResult.PageMap);
        }
    }
}
