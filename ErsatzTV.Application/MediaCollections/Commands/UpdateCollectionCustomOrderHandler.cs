using System.Linq;
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
        UpdateCollectionCustomOrderHandler : MediatR.IRequestHandler<UpdateCollectionCustomOrder,
            Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateCollectionCustomOrderHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            UpdateCollectionCustomOrder request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyUpdateRequest(Collection c, UpdateCollectionCustomOrder request)
        {
            foreach (MediaItemCustomOrder updateItem in request.MediaItemCustomOrders)
            {
                Option<CollectionItem> maybeCollectionItem =
                    c.CollectionItems.FirstOrDefault(ci => ci.MediaItemId == updateItem.MediaItemId);

                maybeCollectionItem.IfSome(ci => ci.CustomIndex = updateItem.CustomIndex);
            }

            if (await _mediaCollectionRepository.Update(c))
            {
                // rebuild all playouts that use this collection
                foreach (int playoutId in await _mediaCollectionRepository.PlayoutIdsUsingCollection(
                    request.CollectionId))
                {
                    await _channel.WriteAsync(new BuildPlayout(playoutId, true));
                }
            }

            return Unit.Default;
        }

        private Task<Validation<BaseError, Collection>> Validate(UpdateCollectionCustomOrder request) =>
            CollectionMustExist(request);

        private Task<Validation<BaseError, Collection>> CollectionMustExist(
            UpdateCollectionCustomOrder request) =>
            _mediaCollectionRepository.Get(request.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));
    }
}
