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

        public static readonly string DatabasePath = Path.Combine(AppDataFolder, "ersatztv.sqlite3");

        public static readonly string LogDatabasePath = Path.Combine(AppDataFolder, "logs.sqlite3");

        public static readonly string LegacyImageCacheFolder = Path.Combine(AppDataFolder, "cache", "images");

        public static readonly string PlexSecretsPath = Path.Combine(AppDataFolder, "plex-secrets.json");

        public static readonly string FFmpegReportsFolder = Path.Combine(AppDataFolder, "ffmpeg-reports");

        public static readonly string ArtworkCacheFolder = Path.Combine(AppDataFolder, "cache", "artwork");

        public static readonly string PosterCacheFolder = Path.Combine(ArtworkCacheFolder, "posters");
        public static readonly string ThumbnailCacheFolder = Path.Combine(ArtworkCacheFolder, "thumbnails");
        public static readonly string LogoCacheFolder = Path.Combine(ArtworkCacheFolder, "logos");
        public static readonly string FanArtCacheFolder = Path.Combine(ArtworkCacheFolder, "fanart");
    }
}
