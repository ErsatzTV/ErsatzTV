using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ErsatzTV.Application.Playouts.Commands;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddEpisodeToCollectionHandler : MediatR.IRequestHandler<AddEpisodeToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddEpisodeToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddEpisodeToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionEpisodeRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionEpisodeRequest(AddEpisodeToCollection request)
        {
            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.EpisodeId))
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

        private async Task<Validation<BaseError, Unit>> Validate(AddEpisodeToCollection request) =>
            (await CollectionMustExist(request), await ValidateEpisode(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddEpisodeToCollection request) =>
            _mediaCollectionRepository.Get(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateEpisode(AddEpisodeToCollection request) =>
            LoadTelevisionEpisode(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Episode does not exist"));

        private Task<Option<int>> LoadTelevisionEpisode(AddEpisodeToCollection request) =>
            _televisionRepository.GetEpisode(request.EpisodeId).MapT(e => e.Id);
    }
}
