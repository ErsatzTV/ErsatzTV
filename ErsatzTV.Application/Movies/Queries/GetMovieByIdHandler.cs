using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Movies.Mapper;

namespace ErsatzTV.Application.Movies.Queries
{
    public class GetMovieByIdHandler : IRequestHandler<GetMovieById, Option<MovieViewModel>>
    {
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMovieRepository _movieRepository;

        public GetMovieByIdHandler(IMovieRepository movieRepository, IMediaSourceRepository mediaSourceRepository)
        {
            _movieRepository = movieRepository;
            _mediaSourceRepository = mediaSourceRepository;
        }

        public async Task<Option<MovieViewModel>> Handle(
            GetMovieById request,
            CancellationToken cancellationToken)
        {
            Option<JellyfinMediaSource> maybeJellyfin = await _mediaSourceRepository.GetAllJellyfin()
                .Map(list => list.HeadOrNone());

            Option<EmbyMediaSource> maybeEmby = await _mediaSourceRepository.GetAllEmby()
                .Map(list => list.HeadOrNone());

            Option<Movie> movie = await _movieRepository.GetMovie(request.Id);
            return movie.Map(m => ProjectToViewModel(m, maybeJellyfin, maybeEmby));
        }
    }
}
