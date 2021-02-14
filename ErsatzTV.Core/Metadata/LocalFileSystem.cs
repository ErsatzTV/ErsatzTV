using System;
using System.IO;
using System.Linq;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalFileSystem : ILocalFileSystem
    {
        public bool IsMediaSourceAccessible(LocalMediaSource localMediaSource) =>
            Directory.Exists(localMediaSource.Folder);

        public Seq<string> FindRelevantVideos(LocalMediaSource localMediaSource)
        {
            Seq<string> allDirectories = Directory
                .GetDirectories(localMediaSource.Folder, "*", SearchOption.AllDirectories)
                .ToSeq()
                .Add(localMediaSource.Folder);

            // remove any directories with an .etvignore file locally, or in any parent directory
            Seq<string> excluded = allDirectories.Filter(ShouldExcludeDirectory);
            Seq<string> relevantDirectories = allDirectories
                .Filter(d => !excluded.Any(d.StartsWith))
                .Filter(d => localMediaSource.MediaType == MediaType.Other || !IsExtrasFolder(d));

            return relevantDirectories
                .Collect(d => Directory.GetFiles(d, "*", SearchOption.TopDirectoryOnly))
                .Filter(file => KnownExtensions.Contains(Path.GetExtension(file)))
                .OrderBy(identity)
                .ToSeq();
        }

        public bool ShouldRefreshMetadata(LocalMediaSource localMediaSource, MediaItem mediaItem)
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(mediaItem.Path);
            bool modified = lastWrite > mediaItem.LastWriteTime.IfNone(DateTime.MinValue);
            return modified // media item has been modified
                   || mediaItem.Metadata == null // media item has no metadata
                   || mediaItem.Metadata.MediaType != localMediaSource.MediaType; // media item is typed incorrectly
        }

        public bool ShouldRefreshPoster(MediaItem mediaItem) =>
            string.IsNullOrWhiteSpace(mediaItem.Poster);

        private static bool ShouldExcludeDirectory(string path) => File.Exists(Path.Combine(path, ".etvignore"));

        // see https://support.emby.media/support/solutions/articles/44001159102-movie-naming
        private static bool IsExtrasFolder(string path) =>
            ExtraFolderNames.Contains(Path.GetFileName(path)?.ToLowerInvariant());

        // @formatter:off
        private static readonly Seq<string> KnownExtensions = Seq(
            ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
            ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts");
        
        private static readonly Seq<string> ExtraFolderNames = Seq(
            "extras", "specials", "shorts", "scenes", "featurettes",
            "behind the scenes", "deleted scenes", "interviews", "trailers");
        // @formatter:on
    }
}
