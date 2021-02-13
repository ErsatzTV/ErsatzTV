using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalPosterProvider : ILocalPosterProvider
    {
        private readonly IImageCache _imageCache;
        private readonly ILogger<LocalPosterProvider> _logger;
        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalPosterProvider(
            IMediaItemRepository mediaItemRepository,
            IImageCache imageCache,
            ILogger<LocalPosterProvider> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _imageCache = imageCache;
            _logger = logger;
        }

        public async Task RefreshPoster(MediaItem mediaItem)
        {
            Option<string> maybePosterPath = mediaItem.Metadata.MediaType switch
            {
                MediaType.Movie => RefreshMoviePoster(mediaItem),
                MediaType.TvShow => RefreshTelevisionPoster(mediaItem),
                _ => None
            };

            await maybePosterPath.Match(
                path => SavePosterToDisk(mediaItem, path),
                Task.CompletedTask);
        }

        private static Option<string> RefreshMoviePoster(MediaItem mediaItem)
        {
            string folder = Path.GetDirectoryName(mediaItem.Path);
            if (folder != null)
            {
                string[] possiblePaths =
                    { "poster.jpg", Path.GetFileNameWithoutExtension(mediaItem.Path) + "-poster.jpg" };
                Option<string> maybePoster =
                    possiblePaths.Map(p => Path.Combine(folder, p)).FirstOrDefault(File.Exists);
                return maybePoster;
            }

            return None;
        }

        private Option<string> RefreshTelevisionPoster(MediaItem mediaItem)
        {
            string folder = Directory.GetParent(Path.GetDirectoryName(mediaItem.Path) ?? string.Empty)?.FullName;
            if (folder != null)
            {
                string[] possiblePaths = { "poster.jpg" };
                Option<string> maybePoster =
                    possiblePaths.Map(p => Path.Combine(folder, p)).FirstOrDefault(File.Exists);
                return maybePoster;
            }

            return None;
        }

        private async Task SavePosterToDisk(MediaItem mediaItem, string posterPath)
        {
            byte[] originalBytes = await File.ReadAllBytesAsync(posterPath);
            Either<BaseError, string> maybeHash = await _imageCache.ResizeAndSaveImage(originalBytes, 220, null);
            await maybeHash.Match(
                hash =>
                {
                    mediaItem.Poster = hash;
                    return _mediaItemRepository.Update(mediaItem);
                },
                error =>
                {
                    _logger.LogWarning("Unable to save poster to disk from {Path}: {Error}", posterPath, error.Value);
                    return Task.CompletedTask;
                });
        }
    }
}
