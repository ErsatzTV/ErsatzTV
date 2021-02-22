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
        AddTelevisionShowToSimpleMediaCollectionHandler : MediatR.IRequestHandler<
            AddTelevisionShowToSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddTelevisionShowToSimpleMediaCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddTelevisionShowToSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddTelevisionShowRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionShowRequest(RequestParameters parameters)
        {
            if (parameters.Collection.TelevisionShows.All(s => s.Id != parameters.ShowToAdd.Id))
            {
                parameters.Collection.TelevisionShows.Add(parameters.ShowToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddTelevisionShowToSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateShow(request))
            .Apply(
                (simpleMediaCollectionToUpdate, show) =>
                    new RequestParameters(simpleMediaCollectionToUpdate, show));

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            AddTelevisionShowToSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollectionWithItems(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Task<Validation<BaseError, TelevisionShow>> ValidateShow(
            AddTelevisionShowToSimpleMediaCollection request) =>
            LoadTelevisionShow(request)
                .Map(v => v.ToValidation<BaseError>("TelevisionShow does not exist"));

        private Task<Option<TelevisionShow>> LoadTelevisionShow(AddTelevisionShowToSimpleMediaCollection request) =>
            _televisionRepository.GetShow(request.TelevisionShowId);

        private record RequestParameters(SimpleMediaCollection Collection, TelevisionShow ShowToAdd);
    }
}
