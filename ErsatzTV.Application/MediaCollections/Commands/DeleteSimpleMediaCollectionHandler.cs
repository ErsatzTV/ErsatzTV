using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class
        DeleteSimpleMediaCollectionHandler : IRequestHandler<DeleteSimpleMediaCollection, Either<BaseError, Task>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public DeleteSimpleMediaCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeleteSimpleMediaCollection request,
            CancellationToken cancellationToken) =>
            (await SimpleMediaCollectionMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int mediaCollectionId) => _mediaCollectionRepository.Delete(mediaCollectionId);

        private async Task<Validation<BaseError, int>> SimpleMediaCollectionMustExist(
            DeleteSimpleMediaCollection deleteMediaCollection) =>
            (await _mediaCollectionRepository.GetSimpleMediaCollection(deleteMediaCollection.SimpleMediaCollectionId))
            .ToValidation<BaseError>(
                $"SimpleMediaCollection {deleteMediaCollection.SimpleMediaCollectionId} does not exist.")
            .Map(c => c.Id);
    }
}
