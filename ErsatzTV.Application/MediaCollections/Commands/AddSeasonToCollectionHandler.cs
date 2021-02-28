using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class AddSeasonToCollectionHandler : MediatR.IRequestHandler<AddSeasonToCollection, Either<BaseError, Unit>>
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

        public Task<Either<BaseError, Unit>> Handle(
            AddSeasonToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(_ => ApplyAddTelevisionSeasonRequest(request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionSeasonRequest(AddSeasonToCollection request) =>
            await _mediaCollectionRepository.AddMediaItem(request.CollectionId, request.SeasonId);

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
