using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddMovieToCollectionHandler : MediatR.IRequestHandler<AddMovieToCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMovieRepository _movieRepository;

        public AddMovieToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMovieRepository movieRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _movieRepository = movieRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddMovieToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddMoviesRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddMoviesRequest(RequestParameters parameters)
        {
            parameters.Collection.MediaItems.Add(parameters.MovieToAdd);
            await _mediaCollectionRepository.Update(parameters.Collection);

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddMovieToCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateMovies(request))
            .Apply(
                (collectionToUpdate, movieToAdd) =>
                    new RequestParameters(collectionToUpdate, movieToAdd));

        private Task<Validation<BaseError, Collection>> SimpleMediaCollectionMustExist(
            AddMovieToCollection updateCollection) =>
            _mediaCollectionRepository.GetCollectionWithItems(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Movie>> ValidateMovies(
            AddMovieToCollection request) =>
            LoadMovie(request)
                .Map(v => v.ToValidation<BaseError>("Movie does not exist"));

        private Task<Option<Movie>> LoadMovie(AddMovieToCollection request) =>
            _movieRepository.GetMovie(request.MovieId);

        private record RequestParameters(Collection Collection, Movie MovieToAdd);
    }
}
