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
    public class AddShowToCollectionHandler : MediatR.IRequestHandler<AddShowToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddShowToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddShowToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionShowRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionShowRequest(AddShowToCollection request)
        {
            var result = new Unit();

            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.ShowId))
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository
                    .PlayoutIdsUsingCollection(request.CollectionId))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return result;
        }

        private async Task<Validation<BaseError, Unit>> Validate(AddShowToCollection request) =>
            (await CollectionMustExist(request), await ValidateShow(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddShowToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateShow(AddShowToCollection request) =>
            LoadTelevisionShow(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Show does not exist"));

        private Task<Option<Show>> LoadTelevisionShow(AddShowToCollection request) =>
            _televisionRepository.GetShow(request.ShowId);
    }
}
