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
    public class UpdateCollectionHandler : MediatR.IRequestHandler<UpdateCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            UpdateCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyUpdateRequest(Collection c, UpdateCollection request)
        {
            c.Name = request.Name;
            await request.UseCustomPlaybackOrder.IfSomeAsync(
                useCustomPlaybackOrder => c.UseCustomPlaybackOrder = useCustomPlaybackOrder);
            if (await _mediaCollectionRepository.Update(c) && request.UseCustomPlaybackOrder.IsSome)
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

        private async Task<Validation<BaseError, Collection>>
            Validate(UpdateCollection request) =>
            (await CollectionMustExist(request), ValidateName(request))
            .Apply((collectionToUpdate, _) => collectionToUpdate);

        private Task<Validation<BaseError, Collection>> CollectionMustExist(
            UpdateCollection updateCollection) =>
            _mediaCollectionRepository.Get(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Validation<BaseError, string> ValidateName(UpdateCollection updateSimpleMediaCollection) =>
            updateSimpleMediaCollection.NotEmpty(c => c.Name)
                .Bind(_ => updateSimpleMediaCollection.NotLongerThan(50)(c => c.Name));
    }
}
