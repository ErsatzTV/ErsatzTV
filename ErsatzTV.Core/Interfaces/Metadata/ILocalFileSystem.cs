using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalFileSystem
    {
        public bool IsMediaSourceAccessible(LocalMediaSource localMediaSource);
        public Seq<string> FindRelevantVideos(LocalMediaSource localMediaSource);
        public bool ShouldRefreshMetadata(LocalMediaSource localMediaSource, MediaItem mediaItem);
        public bool ShouldRefreshPoster(MediaItem mediaItem);
    }
}
