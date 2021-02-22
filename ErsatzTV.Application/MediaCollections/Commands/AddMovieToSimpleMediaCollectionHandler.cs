using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddMovieToSimpleMediaCollectionHandler : MediatR.IRequestHandler<AddMovieToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMovieRepository _movieRepository;

        public AddMovieToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMovieRepository movieRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _movieRepository = movieRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddMovieToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddMoviesRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddMoviesRequest(RequestParameters parameters)
        {
            parameters.Collection.Movies.Add(parameters.MovieToAdd);
            await _mediaCollectionRepository.Update(parameters.Collection);

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddMovieToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateMovies(request))
            .Apply(
                (simpleMediaCollectionToUpdate, movieToAdd) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, movieToAdd));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddMovieToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, MovieMediaItem>> ValidateMovies(
            AddMovieToSimpleMediaCollection request) =>
            LoadMovie(request)
                .Map(v => v.ToValidation<BaseError>("MovieMediaItem does not exist"));

        private Task<Option<MovieMediaItem>> LoadMovie(AddMovieToSimpleMediaCollection request) =>
            _movieRepository.GetMovie(request.MovieId);

        private record RequestParameters(SimpleMediaCollection Collection, MovieMediaItem MovieToAdd);
    }
}
