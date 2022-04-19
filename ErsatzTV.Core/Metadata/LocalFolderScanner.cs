using Bugsnag;
using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public abstract class LocalFolderScanner
{
    public static readonly List<string> VideoFileExtensions = new()
    {
        ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4",
        ".m4p", ".m4v", ".avi", ".wmv", ".mov", ".mkv", ".ts", ".webm"
    };

    public static readonly List<string> AudioFileExtensions = new()
    {
        ".aac", ".alac", ".flac", ".mp3", ".m4a", ".wav", ".wma"
    };

    public static readonly List<string> ImageFileExtensions = new()
    {
        "jpg", "jpeg", "png", "gif", "tbn"
    };

    public static readonly List<string> ExtraFiles = new()
    {
        "behindthescenes", "deleted", "featurette",
        "interview", "scene", "short", "trailer", "other"
    };

    public static readonly List<string> ExtraDirectories = new List<string>
        {
            "behind the scenes", "deleted scenes", "featurettes",
            "interviews", "scenes", "shorts", "trailers", "other",
            "extras", "specials"
        }
        .Map(s => $"{Path.DirectorySeparatorChar}{s}{Path.DirectorySeparatorChar}")
        .ToList();

    private readonly IClient _client;
    private readonly IFFmpegProcessService _ffmpegProcessService;

    private readonly IImageCache _imageCache;

    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILogger _logger;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ITempFilePool _tempFilePool;

    protected LocalFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        IMetadataRepository metadataRepository,
        IMediaItemRepository mediaItemRepository,
        IImageCache imageCache,
        IFFmpegProcessService ffmpegProcessService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger logger)
    {
        _localFileSystem = localFileSystem;
        _localStatisticsProvider = localStatisticsProvider;
        _metadataRepository = metadataRepository;
        _mediaItemRepository = mediaItemRepository;
        _imageCache = imageCache;
        _ffmpegProcessService = ffmpegProcessService;
        _tempFilePool = tempFilePool;
        _client = client;
        _logger = logger;
    }

    protected async Task<Either<BaseError, MediaItemScanResult<T>>> UpdateStatistics<T>(
        MediaItemScanResult<T> mediaItem,
        string ffmpegPath,
        string ffprobePath)
        where T : MediaItem
    {
        try
        {
            MediaVersion version = mediaItem.Item.GetHeadVersion();

            string path = version.MediaFiles.Head().Path;

            if (version.DateUpdated != _localFileSystem.GetLastWriteTime(path) || !version.Streams.Any())
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", path);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(ffmpegPath, ffprobePath, mediaItem.Item);
                refreshResult.Match(
                    result =>
                    {
                        if (result)
                        {
                            mediaItem.IsUpdated = true;
                        }
                    },
                    error =>
                        _logger.LogWarning(
                            "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                            "Statistics",
                            path,
                            error.Value));
            }

            return mediaItem;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.Message);
        }
    }

    protected async Task<bool> RefreshArtwork(
        string artworkFile,
        Domain.Metadata metadata,
        ArtworkKind artworkKind,
        Option<string> ffmpegPath,
        Option<int> attachedPicIndex,
        CancellationToken cancellationToken)
    {
        DateTime lastWriteTime = _localFileSystem.GetLastWriteTime(artworkFile);

        metadata.Artwork ??= new List<Artwork>();

        Option<Artwork> maybeArtwork = metadata.Artwork.FirstOrDefault(a => a.ArtworkKind == artworkKind);

        bool shouldRefresh = maybeArtwork.Match(
            artwork => lastWriteTime.Subtract(artwork.DateUpdated) > TimeSpan.FromSeconds(1),
            true);

        if (shouldRefresh)
        {
            try
            {
                _logger.LogDebug("Refreshing {Attribute} from {Path}", artworkKind, artworkFile);

                string sourcePath = artworkFile;
                if (await _metadataRepository.CloneArtwork(
                        metadata,
                        maybeArtwork,
                        artworkKind,
                        sourcePath,
                        lastWriteTime))
                {
                    return true;
                }

                // if ffmpeg path is passed, we need pre-processing
                foreach (string path in ffmpegPath)
                {
                    artworkFile = await attachedPicIndex.Match(
                        async picIndex =>
                        {
                            // extract attached pic (and convert to png)
                            string tempName = _tempFilePool.GetNextTempFile(TempFileCategory.CoverArt);
                            Command process = _ffmpegProcessService.ExtractAttachedPicAsPng(
                                path,
                                artworkFile,
                                picIndex,
                                tempName);

                            await process.ExecuteAsync(cancellationToken);

                            return tempName;
                        },
                        async () =>
                        {
                            // no attached pic index means convert to png
                            string tempName = _tempFilePool.GetNextTempFile(TempFileCategory.CoverArt);
                            Command process = _ffmpegProcessService.ConvertToPng(path, artworkFile, tempName);

                            await process.ExecuteAsync(cancellationToken);

                            return tempName;
                        });
                }

                Either<BaseError, string> maybeCacheName =
                    await _imageCache.CopyArtworkToCache(artworkFile, artworkKind);

                return await maybeCacheName.Match(
                    async cacheName =>
                    {
                        await maybeArtwork.Match(
                            async artwork =>
                            {
                                artwork.Path = cacheName;
                                artwork.SourcePath = sourcePath;
                                artwork.DateUpdated = lastWriteTime;

                                if (metadata is SongMetadata)
                                {
                                    artwork.BlurHash43 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 4, 3);
                                    artwork.BlurHash54 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 5, 4);
                                    artwork.BlurHash64 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 6, 4);
                                }

                                await _metadataRepository.UpdateArtworkPath(artwork);
                            },
                            async () =>
                            {
                                var artwork = new Artwork
                                {
                                    Path = cacheName,
                                    SourcePath = sourcePath,
                                    DateAdded = DateTime.UtcNow,
                                    DateUpdated = lastWriteTime,
                                    ArtworkKind = artworkKind
                                };

                                if (metadata is SongMetadata)
                                {
                                    artwork.BlurHash43 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 4, 3);
                                    artwork.BlurHash54 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 5, 4);
                                    artwork.BlurHash64 = _imageCache.CalculateBlurHash(cacheName, artworkKind, 6, 4);
                                }

                                metadata.Artwork.Add(artwork);
                                await _metadataRepository.AddArtwork(metadata, artwork);
                            });

                        return true;
                    },
                    error =>
                    {
                        _logger.LogDebug("Failed to cache artwork from {Path}: {Error}", artworkFile, error.Value);
                        return Task.FromResult(false);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error refreshing artwork");
                _client.Notify(ex);
            }
        }

        return false;
    }

    protected Task<List<int>> FlagFileNotFound(LibraryPath libraryPath, string path) =>
        _mediaItemRepository.FlagFileNotFound(libraryPath, path);

    protected async Task<Either<BaseError, MediaItemScanResult<T>>> FlagNormal<T>(MediaItemScanResult<T> result)
        where T : MediaItem
    {
        try
        {
            T mediaItem = result.Item;
            if (mediaItem.State != MediaItemState.Normal)
            {
                await _mediaItemRepository.FlagNormal(mediaItem);
                result.IsUpdated = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    protected bool ShouldIncludeFolder(string folder) =>
        !Path.GetFileName(folder).StartsWith('.') &&
        !_localFileSystem.FileExists(Path.Combine(folder, ".etvignore"));
}
