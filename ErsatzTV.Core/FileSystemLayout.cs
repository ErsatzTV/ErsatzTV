using System;
using System.IO;

namespace ErsatzTV.Core
{
    public static class FileSystemLayout
    {
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create),
            "ersatztv");

        // TODO: find a different spot for this; configurable?
        public static readonly string TranscodeFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create),
            "etv-transcode");

        public static readonly string DatabasePath = Path.Combine(AppDataFolder, "ersatztv.sqlite3");

        public static readonly string LogDatabasePath = Path.Combine(AppDataFolder, "logs.sqlite3");

        public static readonly string LegacyImageCacheFolder = Path.Combine(AppDataFolder, "cache", "images");
        public static readonly string ResourcesCacheFolder = Path.Combine(AppDataFolder, "cache", "resources");

        public static readonly string PlexSecretsPath = Path.Combine(AppDataFolder, "plex-secrets.json");
        public static readonly string JellyfinSecretsPath = Path.Combine(AppDataFolder, "jellyfin-secrets.json");
        public static readonly string EmbySecretsPath = Path.Combine(AppDataFolder, "emby-secrets.json");

        public static readonly string FFmpegReportsFolder = Path.Combine(AppDataFolder, "ffmpeg-reports");
        public static readonly string SearchIndexFolder = Path.Combine(AppDataFolder, "search-index");

        public static readonly string ArtworkCacheFolder = Path.Combine(AppDataFolder, "cache", "artwork");

        public static readonly string PosterCacheFolder = Path.Combine(ArtworkCacheFolder, "posters");
        public static readonly string ThumbnailCacheFolder = Path.Combine(ArtworkCacheFolder, "thumbnails");
        public static readonly string LogoCacheFolder = Path.Combine(ArtworkCacheFolder, "logos");
        public static readonly string FanArtCacheFolder = Path.Combine(ArtworkCacheFolder, "fanart");
        public static readonly string WatermarkCacheFolder = Path.Combine(ArtworkCacheFolder, "watermarks");
    }
}
