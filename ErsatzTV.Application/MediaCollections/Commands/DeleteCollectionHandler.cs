using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public class DeleteCollectionHandler : IRequestHandler<DeleteCollection, Either<BaseError, Task>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;

        public DeleteCollectionHandler(IMediaCollectionRepository mediaCollectionRepository) =>
            _mediaCollectionRepository = mediaCollectionRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeleteCollection request,
            CancellationToken cancellationToken) =>
            (await CollectionMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int mediaCollectionId) => _mediaCollectionRepository.Delete(mediaCollectionId);

        private async Task<Validation<BaseError, int>> CollectionMustExist(
            DeleteCollection deleteMediaCollection) =>
            (await _mediaCollectionRepository.Get(deleteMediaCollection.CollectionId))
            .ToValidation<BaseError>(
                $"Collection {deleteMediaCollection.CollectionId} does not exist.")
            .Map(c => c.Id);
    }
}
