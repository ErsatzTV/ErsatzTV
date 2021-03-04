using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddEpisodeToCollectionHandler : IRequestHandler<AddEpisodeToCollection,
            Either<BaseError, CollectionUpdateResult>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddEpisodeToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, CollectionUpdateResult>> Handle(
            AddEpisodeToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionEpisodeRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<CollectionUpdateResult> ApplyAddTelevisionEpisodeRequest(AddEpisodeToCollection request)
        {
            var result = new CollectionUpdateResult();

            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.EpisodeId))
            {
                result.ModifiedPlayoutIds =
                    await _mediaCollectionRepository.PlayoutIdsUsingCollection(request.CollectionId);
            }

            return result;
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
