using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalPosterProvider : ILocalPosterProvider
    {
        private readonly IMediaItemRepository _mediaItemRepository;

        public LocalPosterProvider(IMediaItemRepository mediaItemRepository) =>
            _mediaItemRepository = mediaItemRepository;

        public Task RefreshPoster(MediaItem mediaItem)
        {
            Option<string> maybePoster = mediaItem.Metadata.MediaType switch
            {
                MediaType.Movie => RefreshMoviePoster(mediaItem),
                MediaType.TvShow => RefreshTelevisionPoster(mediaItem),
                _ => None
            };

            return maybePoster.Match(
                path =>
                {
                    mediaItem.PosterPath = path;
                    return _mediaItemRepository.Update(mediaItem);
                },
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

        private Option<string> RefreshTelevisionPoster(MediaItem mediaItem) => None;
    }
}
