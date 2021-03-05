using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddMovieToCollectionHandler : MediatR.IRequestHandler<AddMovieToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMovieRepository _movieRepository;

        public AddMovieToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMovieRepository movieRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _movieRepository = movieRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddMovieToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddMoviesRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddMoviesRequest(AddMovieToCollection request)
        {
            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.MovieId))
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository
                    .PlayoutIdsUsingCollection(request.CollectionId))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
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
