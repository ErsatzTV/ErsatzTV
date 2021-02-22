using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using static ErsatzTV.Application.Movies.Mapper;

namespace ErsatzTV.Application.Movies.Queries
{
    public class GetMovieByIdHandler : IRequestHandler<GetMovieById, Option<MovieViewModel>>
    {
        private readonly IMovieRepository _movieRepository;

        public GetMovieByIdHandler(IMovieRepository movieRepository) =>
            _movieRepository = movieRepository;

        public Task<Option<MovieViewModel>> Handle(
            GetMovieById request,
            CancellationToken cancellationToken) =>
            _movieRepository.GetMovie(request.Id).MapT(ProjectToViewModel);
    }
}
