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
        AddTelevisionSeasonToSimpleMediaCollectionHandler : MediatR.IRequestHandler<
            AddTelevisionSeasonToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddTelevisionSeasonToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddTelevisionSeasonToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddTelevisionSeasonRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionSeasonRequest(RequestParameters parameters)
        {
            if (parameters.Collection.TelevisionSeasons.All(s => s.Id != parameters.SeasonToAdd.Id))
            {
                parameters.Collection.TelevisionSeasons.Add(parameters.SeasonToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddTelevisionSeasonToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateSeason(request))
            .Apply(
                (simpleMediaCollectionToUpdate, season) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, season));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddTelevisionSeasonToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, TelevisionSeason>> ValidateSeason(
            AddTelevisionSeasonToSimpleMediaCollection request) =>
            LoadTelevisionSeason(request)
                .Map(v => v.ToValidation<BaseError>("TelevisionSeason does not exist"));

        private Task<Option<TelevisionSeason>> LoadTelevisionSeason(
            AddTelevisionSeasonToSimpleMediaCollection request) =>
            _televisionRepository.GetSeason(request.TelevisionSeasonId);

        private record RequestParameters(SimpleMediaCollection Collection, TelevisionSeason SeasonToAdd);
    }
}
