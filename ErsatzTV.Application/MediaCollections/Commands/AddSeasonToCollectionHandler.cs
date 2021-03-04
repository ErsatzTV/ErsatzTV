using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddSeasonToCollectionHandler : IRequestHandler<AddSeasonToCollection, Either<BaseError, CollectionUpdateResult>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddSeasonToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, CollectionUpdateResult>> Handle(
            AddSeasonToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionSeasonRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<CollectionUpdateResult> ApplyAddTelevisionSeasonRequest(AddSeasonToCollection request)
        {
            var result = new CollectionUpdateResult();

            if (await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.SeasonId))
            {
                result.ModifiedPlayoutIds =
                    await _mediaCollectionRepository.PlayoutIdsUsingCollection(request.CollectionId);
            }

            return result;
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
