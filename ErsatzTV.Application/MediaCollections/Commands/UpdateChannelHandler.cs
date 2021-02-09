using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        UpdateSimpleMediaCollectionHandler : MediatR.IRequestHandler<UpdateSimpleMediaCollection,
            Either<BaseError, Unit>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public UpdateSimpleMediaCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public Task<Either<BaseError, Unit>> Handle(
            UpdateSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .MapT(c => ApplyUpdateRequest(c, request))
                .Bind(v => v.ToEitherAsync());

        private async Task<Unit> ApplyUpdateRequest(SimpleMediaCollection c, UpdateSimpleMediaCollection update)
        {
            c.Name = update.Name;
            await _mediaCollectionRepository.Update(c);
            return Unit.Default;
        }

        private async Task<Validation<BaseError, SimpleMediaCollection>>
            Validate(UpdateSimpleMediaCollection request) =>
            (await SimpleMediaCollectionMustExist(request), ValidateName(request))
            .Apply((simpleMediaCollectionToUpdate, _) => simpleMediaCollectionToUpdate);

        private Task<Validation<BaseError, SimpleMediaCollection>> SimpleMediaCollectionMustExist(
            UpdateSimpleMediaCollection updateSimpleMediaCollection) =>
            _mediaCollectionRepository.GetSimpleMediaCollection(updateSimpleMediaCollection.MediaCollectionId)
                .Map(v => v.ToValidation<BaseError>("SimpleMediaCollection does not exist."));

        private Validation<BaseError, string> ValidateName(UpdateSimpleMediaCollection updateSimpleMediaCollection) =>
            updateSimpleMediaCollection.NotEmpty(c => c.Name)
                .Bind(_ => updateSimpleMediaCollection.NotLongerThan(50)(c => c.Name));
    }
}
