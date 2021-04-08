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
        AddArtistToCollectionHandler : MediatR.IRequestHandler<AddArtistToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IArtistRepository _artistRepository;

        public AddArtistToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IArtistRepository artistRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _artistRepository = artistRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddArtistToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddArtistRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddArtistRequest(AddArtistToCollection request)
        {
            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.ArtistId))
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

        private async Task<Validation<BaseError, Unit>> Validate(AddArtistToCollection request) =>
            (await CollectionMustExist(request), await ValidateArtist(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddArtistToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateArtist(AddArtistToCollection request) =>
            LoadArtist(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Music video does not exist"));

        private Task<Option<Artist>> LoadArtist(AddArtistToCollection request) =>
            _artistRepository.GetArtist(request.ArtistId);
    }
}
