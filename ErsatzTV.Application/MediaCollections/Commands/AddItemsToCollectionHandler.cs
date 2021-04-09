using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddItemsToCollectionHandler : MediatR.IRequestHandler<AddItemsToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddItemsToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMovieRepository movieRepository,
            ITelevisionRepository televisionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _movieRepository = movieRepository;
            _televisionRepository = televisionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddItemsToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddItemsRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddItemsRequest(AddItemsToCollection request)
        {
            var allItems = request.MovieIds
                .Append(request.ShowIds)
                .Append(request.ArtistIds)
                .Append(request.MusicVideoIds)
                .ToList();

            if (await _mediaCollectionRepository.AddMediaItems(request.CollectionId, allItems))
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

        private async Task<Validation<BaseError, Unit>> Validate(AddItemsToCollection request) =>
            (await CollectionMustExist(request), await ValidateMovies(request), await ValidateShows(request))
            .Apply((_, _, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddItemsToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateMovies(AddItemsToCollection request) =>
            _movieRepository.AllMoviesExist(request.MovieIds)
                .Map(Optional)
                .Filter(v => v == true)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Movie does not exist"));

        private Task<Validation<BaseError, Unit>> ValidateShows(AddItemsToCollection request) =>
            _televisionRepository.AllShowsExist(request.ShowIds)
                .Map(Optional)
                .Filter(v => v == true)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Show does not exist"));
    }
}
