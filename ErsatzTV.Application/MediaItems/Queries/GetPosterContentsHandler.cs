using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Application.Images;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Winista.Mime;
using static LanguageExt.Prelude;

namespace ErsatzTV.Application.MediaItems.Queries
{
    public class GetPosterContentsHandler : IRequestHandler<GetPosterContents, Either<BaseError, ImageViewModel>>
    {
        private static readonly MimeTypes MimeTypes = new();

        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly IMemoryCache _memoryCache;

        public GetPosterContentsHandler(IMediaItemRepository mediaItemRepository, IMemoryCache memoryCache)
        {
            _mediaItemRepository = mediaItemRepository;
            _memoryCache = memoryCache;
        }

        public Task<Either<BaseError, ImageViewModel>> Handle(
            GetPosterContents request,
            CancellationToken cancellationToken) =>
            Validate(request)
                .Map(v => v.ToEither<MediaItem>())
                .BindT(GetPoster);

        private async Task<Either<BaseError, ImageViewModel>> GetPoster(MediaItem mediaItem)
        {
            try
            {
                return await _memoryCache.GetOrCreateAsync(
                    mediaItem.Poster,
                    async entry =>
                    {
                        entry.SlidingExpiration = TimeSpan.FromHours(1);

                        byte[] contents = await File.ReadAllBytesAsync(mediaItem.Poster);
                        MimeType mimeType = MimeTypes.GetMimeType(contents);
                        return new ImageViewModel(contents, mimeType.Name);
                    });
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private Task<Validation<BaseError, MediaItem>> Validate(GetPosterContents request) =>
            MediaItemMustExist(request).BindT(PosterPathMustExist);

        private async Task<Validation<BaseError, MediaItem>> MediaItemMustExist(GetPosterContents request) =>
            (await _mediaItemRepository.Get(request.MediaItemId))
            .ToValidation<BaseError>($"MediaItem {request.MediaItemId} does not exist.");

        private static Validation<BaseError, MediaItem> PosterPathMustExist(MediaItem mediaItem) =>
            Optional(mediaItem.Poster)
                .Filter(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .Map(_ => mediaItem)
                .ToValidation<BaseError>($"MediaItem {mediaItem.Id} does not have a poster");
    }
}
