using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddMovieToCollectionHandler : IRequestHandler<AddMovieToCollection, Either<BaseError, CollectionUpdateResult>>
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

        public Task<Either<BaseError, CollectionUpdateResult>> Handle(
            AddMovieToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddMoviesRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<CollectionUpdateResult> ApplyAddMoviesRequest(AddMovieToCollection request)
        {
            var result = new CollectionUpdateResult();

            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.MovieId))
            {
                result.ModifiedPlayoutIds =
                    await _mediaCollectionRepository.PlayoutIdsUsingCollection(request.CollectionId);
            }

            return result;
        }

        private async Task<Validation<BaseError, Unit>> Validate(AddMovieToCollection request) =>
            (await CollectionMustExist(request), await ValidateMovies(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddMovieToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateMovies(AddMovieToCollection request) =>
            LoadMovie(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Movie does not exist"));

        private Task<Option<Movie>> LoadMovie(AddMovieToCollection request) =>
            _movieRepository.GetMovie(request.MovieId);
    }
}
