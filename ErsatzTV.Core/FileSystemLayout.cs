namespace ErsatzTV.Core;

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

    public static readonly string LogsFolder = Path.Combine(AppDataFolder, "logs");

    public static readonly string DatabasePath = Path.Combine(AppDataFolder, "ersatztv.sqlite3");

    public static readonly string LogFilePath = Path.Combine(LogsFolder, "ersatztv.log");

    public static readonly string LegacyImageCacheFolder = Path.Combine(AppDataFolder, "cache", "images");
    public static readonly string ResourcesCacheFolder = Path.Combine(AppDataFolder, "cache", "resources");

    public static readonly string PlexSecretsPath = Path.Combine(AppDataFolder, "plex-secrets.json");
    public static readonly string JellyfinSecretsPath = Path.Combine(AppDataFolder, "jellyfin-secrets.json");
    public static readonly string EmbySecretsPath = Path.Combine(AppDataFolder, "emby-secrets.json");

    public static readonly string FFmpegReportsFolder = Path.Combine(AppDataFolder, "ffmpeg-reports");
    public static readonly string SearchIndexFolder = Path.Combine(AppDataFolder, "search-index");
    public static readonly string TempFilePoolFolder = Path.Combine(AppDataFolder, "temp-pool");

    public static readonly string ArtworkCacheFolder = Path.Combine(AppDataFolder, "cache", "artwork");

    public static readonly string PosterCacheFolder = Path.Combine(ArtworkCacheFolder, "posters");
    public static readonly string ThumbnailCacheFolder = Path.Combine(ArtworkCacheFolder, "thumbnails");
    public static readonly string LogoCacheFolder = Path.Combine(ArtworkCacheFolder, "logos");
    public static readonly string FanArtCacheFolder = Path.Combine(ArtworkCacheFolder, "fanart");
    public static readonly string WatermarkCacheFolder = Path.Combine(ArtworkCacheFolder, "watermarks");

    public static readonly string StreamsCacheFolder = Path.Combine(AppDataFolder, "cache", "streams");

    public static readonly string SubtitleCacheFolder = Path.Combine(StreamsCacheFolder, "subtitles");
    public static readonly string FontsCacheFolder = Path.Combine(StreamsCacheFolder, "fonts");

    public static readonly string TemplatesFolder = Path.Combine(AppDataFolder, "templates");

    public static readonly string MusicVideoCreditsTemplatesFolder =
        Path.Combine(TemplatesFolder, "music-video-credits");

    public static readonly string ScriptsFolder = Path.Combine(AppDataFolder, "scripts");

    public static readonly string MultiEpisodeShuffleTemplatesFolder =
        Path.Combine(ScriptsFolder, "multi-episode-shuffle");

    public static readonly string AudioStreamSelectorScriptsFolder =
        Path.Combine(ScriptsFolder, "audio-stream-selector");
}
