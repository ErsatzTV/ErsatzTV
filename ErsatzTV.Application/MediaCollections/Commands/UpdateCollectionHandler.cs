using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class UpdateCollectionHandler : MediatR.IRequestHandler<UpdateCollection, Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, Unit>> Handle(
            UpdateCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyUpdateRequest(Collection c, UpdateCollection update)
        {
            c.Name = update.Name;
            await _mediaCollectionRepository.Update(c);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, Collection>>
            Validate(UpdateCollection request) =>
            (await CollectionMustExist(request), ValidateName(request))
            .Apply((collectionToUpdate, _) => collectionToUpdate);

        private Task<Validation<BaseError, Collection>> CollectionMustExist(
            UpdateCollection updateCollection) =>
            _mediaCollectionRepository.Get(updateCollection.CollectionId)
                .Map(v => v.ToValidation<BaseError>("Collection does not exist."));

        private Validation<BaseError, string> ValidateName(UpdateCollection updateSimpleMediaCollection) =>
            updateSimpleMediaCollection.NotEmpty(c => c.Name)
                .Bind(_ => updateSimpleMediaCollection.NotLongerThan(50)(c => c.Name));
    }
}
