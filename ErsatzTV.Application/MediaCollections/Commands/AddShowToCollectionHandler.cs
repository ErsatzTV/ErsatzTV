using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class AddShowToCollectionHandler : MediatR.IRequestHandler<AddShowToCollection, Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public AddShowToCollectionHandler(
            IMediaCollectionRepository mediaCollectionRepository,
            ITelevisionRepository televisionRepository)
        {
            _mediaCollectionRepository = mediaCollectionRepository;
            _televisionRepository = televisionRepository;
        }

        public Task<Either<BaseError, Unit>> Handle(
            AddShowToCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(ApplyAddTelevisionShowRequest)
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyAddTelevisionShowRequest(RequestParameters parameters)
        {
            if (parameters.Collection.MediaItems.All(s => s.Id != parameters.ShowToAdd.Id))
            {
                parameters.Collection.MediaItems.Add(parameters.ShowToAdd);
                await _mediaCollectionRepository.Update(parameters.Collection);
            }

            return Unit.Default;
        }

        private async Task<Validation<BaseError, RequestParameters>>
            Validate(AddShowToCollection request) =>
            (await SimpleMediaCollectionMustExist(request), await ValidateShow(request))
            .Apply(
                (collectionToUpdate, show) =>
                    new RequestParameters(collectionToUpdate, show));

        private Task<Validation<BaseError, Collection>> SimpleMediaCollectionMustExist(
            AddShowToCollection updateCollection) =>
            _mediaCollectionRepository.GetCollectionWithItems(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Task<Validation<BaseError, Show>> ValidateShow(
            AddShowToCollection request) =>
            LoadTelevisionShow(request)
                .Map(v => v.ToValidation<BaseError>("Show does not exist"));

        private Task<Option<Show>> LoadTelevisionShow(AddShowToCollection request) =>
            _televisionRepository.GetShow(request.ShowId);

        private record RequestParameters(Collection Collection, Show ShowToAdd);
    }
}
