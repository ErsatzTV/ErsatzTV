using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaSources.Commands
{
    public class
        DeleteLocalMediaSourceHandler : IRequestHandler<DeleteLocalMediaSource, Either<BaseError, Task>>
    {
        private readonly IMediaCollectionRepository _mediaCollectionRepository;
        private readonly IMediaSourceRepository _mediaSourceRepository;

        public DeleteLocalMediaSourceHandler(
            IMediaSourceRepository mediaSourceRepository,
            IMediaCollectionRepository mediaCollectionRepository)
        {
            _mediaSourceRepository = mediaSourceRepository;
            _mediaCollectionRepository = mediaCollectionRepository;
        }

        public async Task<Either<BaseError, Task>> Handle(
            DeleteLocalMediaSource request,
            CancellationToken cancellationToken) =>
            (await MediaSourceMustExist(request))
            .Map(DoDeletion)
            .ToEither<Task>();

        private async Task DoDeletion(LocalMediaSource mediaSource)
        {
            await _mediaSourceRepository.Delete(mediaSource.Id);
            if (mediaSource.MediaType == MediaType.TvShow)
            {
                await _mediaCollectionRepository.DeleteEmptyTelevisionCollections();
            }
        }

        private async Task<Validation<BaseError, LocalMediaSource>> MediaSourceMustExist(
            DeleteLocalMediaSource deleteMediaSource) =>
            (await _mediaSourceRepository.Get(deleteMediaSource.LocalMediaSourceId))
            .OfType<LocalMediaSource>()
            .HeadOrNone()
            .ToValidation<BaseError>(
                $"Local media source {deleteMediaSource.LocalMediaSourceId} does not exist.");
    }
}
