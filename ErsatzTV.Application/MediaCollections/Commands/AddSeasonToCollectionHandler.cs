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
        AddSeasonToCollectionHandler : MediatR.IRequestHandler<AddSeasonToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddSeasonToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddSeasonToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionSeasonRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionSeasonRequest(AddSeasonToCollection request)
        {
            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.SeasonId))
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

        private async Task<Validation<BaseError, Unit>> Validate(AddSeasonToCollection request) =>
            (await CollectionMustExist(request), await ValidateSeason(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddSeasonToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateSeason(AddSeasonToCollection request) =>
            LoadTelevisionSeason(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Season does not exist"));

        private Task<Option<Season>> LoadTelevisionSeason(
            AddSeasonToCollection request) =>
            _televisionRepository.GetSeason(request.SeasonId);
    }
}
