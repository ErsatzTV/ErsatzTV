using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaItems.Commands
{
    public class DeleteMediaItemHandler : IRequestHandler<DeleteMediaItem, Either<BaseError, Task>>
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public DeleteMediaItemHandler(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public async Task<Either<BaseError, Task>> Handle(
            DeleteMediaItem request,
            CancellationToken cancellationToken) =>
            (await MediaItemMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private Task DoDeletion(int mediaItemId) => _mediaItemRepository.Delete(mediaItemId);

        private async Task<Validation<BaseError, int>> MediaItemMustExist(DeleteMediaItem deleteMediaItem) =>
            (await _mediaItemRepository.Get(deleteMediaItem.MediaItemId))
            .ToValidation<BaseError>($"MediaItem {deleteMediaItem.MediaItemId} does not exist.")
            .Map(c => c.Id);
    }
}
