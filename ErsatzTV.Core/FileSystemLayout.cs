using System;
using System.IO;

namespace ErsatzTV.Core
{
    public static class FileSystemLayout
    {
        public static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ersatztv");

        public static readonly string DatabasePath = Path.Combine(AppDataFolder, "ersatztv.sqlite3");

        public static readonly string ImageCacheFolder = Path.Combine(AppDataFolder, "cache", "images");

        public static readonly string PlexSecretsPath = Path.Combine(AppDataFolder, "plex-secrets.json");
    }
}
