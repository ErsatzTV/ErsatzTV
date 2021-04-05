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
        AddMusicVideoToCollectionHandler : MediatR.IRequestHandler<AddMusicVideoToCollection, Either<BaseError, Unit>>
    {
        private readonly ChannelWriter<IBackgroundServiceRequest> _channel;
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMusicVideoRepository _musicVideoRepository;

        public AddMusicVideoToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            IMusicVideoRepository musicVideoRepository,
            ChannelWriter<IBackgroundServiceRequest> channel)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _musicVideoRepository = musicVideoRepository;
            _channel = channel;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddMusicVideoToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddMusicVideoRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddMusicVideoRequest(AddMusicVideoToCollection request)
        {
            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.MusicVideoId))
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

        private async Task<Validation<BaseError, Unit>> Validate(AddMusicVideoToCollection request) =>
            (await CollectionMustExist(request), await ValidateMusicVideo(request))
            .Apply((_, _) => Unit.Default);

        private Task<Validation<BaseError, Unit>> CollectionMustExist(AddMusicVideoToCollection request) =>
            _mediaCollectionRepository.GetCollectionWithItems(request.CollectionId)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Unit>> ValidateMusicVideo(AddMusicVideoToCollection request) =>
            LoadMusicVideo(request)
                .MapT(_ => Unit.Default)
                .Map(v => v.ToValidation<BaseError>("Music video does not exist"));

        private Task<Option<MusicVideo>> LoadMusicVideo(AddMusicVideoToCollection request) =>
            _musicVideoRepository.GetMusicVideo(request.MusicVideoId);
    }
}
