using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.MediaCards.Mapper;

namespace ErsatzTV.Application.MediaCards.Queries
{
    public class
        GetMovieCardsHandler : IRequestHandler<GetMovieCards, MovieCardResultsViewModel>
    {
        private readonly IMovieRepository _movieRepository;

        public GetMovieCardsHandler(IMovieRepository movieRepository) => _movieRepository = movieRepository;

        public async Task<MovieCardResultsViewModel> Handle(GetMovieCards request, CancellationToken cancellationToken)
        {
            int count = await _movieRepository.GetMovieCount();

            List<MovieCardViewModel> results = await _movieRepository
                .GetPagedMovies(request.PageNumber, request.PageSize)
                .Map(list => list.Map(ProjectToViewModel).ToList());

            return new MovieCardResultsViewModel(count, results);
        }
    }
}
