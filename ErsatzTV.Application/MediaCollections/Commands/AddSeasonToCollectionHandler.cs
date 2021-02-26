using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        AddSeasonToCollectionHandler : MediatR.IRequestHandler<AddSeasonToCollection,
            Either<BaseError, Unit>>
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
                .MapT(ApplyAddTelevisionSeasonRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionSeasonRequest(RequestParameters parameters)
        {
            if (parameters.Collection.MediaItems.All(s => s.Id != parameters.SeasonToAdd.Id))
            {
                parameters.Collection.MediaItems.Add(parameters.SeasonToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddSeasonToCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateSeason(request))
            .Apply(
                (collectionToUpdate, season) =>
                    new RequestParameters(collectionToUpdate, season));

        private Task<Validation<BaseError, Collection>> SimpleMediaCollectionMustExist(
            AddSeasonToCollection updateCollection) =>
            _mediaCollectionRepository.GetCollectionWithItems(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Season>> ValidateSeason(
            AddSeasonToCollection request) =>
            LoadTelevisionSeason(request)
                .Map(v => v.ToValidation<BaseError>("Season does not exist"));

        private Task<Option<Season>> LoadTelevisionSeason(
            AddSeasonToCollection request) =>
            _televisionRepository.GetSeason(request.SeasonId);

        private record RequestParameters(Collection Collection, Season SeasonToAdd);
    }
}
