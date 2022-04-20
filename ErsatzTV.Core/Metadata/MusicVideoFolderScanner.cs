using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata;

public class MusicVideoFolderScanner : LocalFolderScanner, IMusicVideoFolderScanner
{
    private readonly IArtistRepository _artistRepository;
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<MusicVideoFolderScanner> _logger;
    private readonly IMediator _mediator;
    private readonly IMusicVideoRepository _musicVideoRepository;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public MusicVideoFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalMetadataProvider localMetadataProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IArtistRepository artistRepository,
        IMusicVideoRepository musicVideoRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IMediator mediator,
        IFFmpegProcessService ffmpegProcessService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<MusicVideoFolderScanner> logger) : base(
        localFileSystem,
        localStatisticsProvider,
        metadataRepository,
        mediaItemRepository,
        imageCache,
        ffmpegProcessService,
        tempFilePool,
        client,
        logger)
    {
        _localFileSystem = localFileSystem;
        _localMetadataProvider = localMetadataProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _artistRepository = artistRepository;
        _musicVideoRepository = musicVideoRepository;
        _libraryRepository = libraryRepository;
        _mediator = mediator;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffmpegPath,
        string ffprobePath,
        decimal progressMin,
        decimal progressMax,
        CancellationToken cancellationToken)
    {
        decimal progressSpread = progressMax - progressMin;

        var allArtistFolders = _localFileSystem.ListSubdirectories(libraryPath.Path)
            .Filter(ShouldIncludeFolder)
            .OrderBy(identity)
            .ToList();

        foreach (string artistFolder in allArtistFolders)
        {
            // _logger.LogDebug("Scanning artist folder {Folder}", artistFolder);

            decimal percentCompletion = (decimal)allArtistFolders.IndexOf(artistFolder) / allArtistFolders.Count;
            await _mediator.Publish(
                new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread));

            Either<BaseError, MediaItemScanResult<Artist>> maybeArtist =
                await FindOrCreateArtist(libraryPath.Id, artistFolder)
                    .BindT(artist => UpdateMetadataForArtist(artist, artistFolder))
                    .BindT(
                        artist => UpdateArtworkForArtist(
                            artist,
                            artistFolder,
                            ArtworkKind.Thumbnail,
                            cancellationToken))
                    .BindT(
                        artist => UpdateArtworkForArtist(artist, artistFolder, ArtworkKind.FanArt, cancellationToken));

            await maybeArtist.Match(
                async result =>
                {
                    await ScanMusicVideos(
                        libraryPath,
                        ffmpegPath,
                        ffprobePath,
                        result.Item,
                        artistFolder,
                        cancellationToken);

                    if (result.IsAdded)
                    {
                        await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                    }
                    else if (result.IsUpdated)
                    {
                        await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                    }
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error processing artist in folder {Folder}: {Error}",
                        artistFolder,
                        error.Value);
                    return Task.FromResult(Unit.Default);
                });
        }

        foreach (string path in await _musicVideoRepository.FindOrphanPaths(libraryPath))
        {
            _logger.LogInformation("Removing improperly named music video at {Path}", path);
            List<int> musicVideoIds = await _musicVideoRepository.DeleteByPath(libraryPath, path);
            await _searchIndex.RemoveItems(musicVideoIds);
        }

        foreach (string path in await _musicVideoRepository.FindMusicVideoPaths(libraryPath))
        {
            if (!_localFileSystem.FileExists(path))
            {
                _logger.LogInformation("Flagging missing music video at {Path}", path);
                List<int> musicVideoIds = await FlagFileNotFound(libraryPath, path);
                await _searchIndex.RebuildItems(_searchRepository, musicVideoIds);
            }
            else if (Path.GetFileName(path).StartsWith("._"))
            {
                _logger.LogInformation("Removing dot underscore file at {Path}", path);
                List<int> musicVideoIds = await _musicVideoRepository.DeleteByPath(libraryPath, path);
                await _searchIndex.RemoveItems(musicVideoIds);
            }
        }

        await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

        List<int> artistIds = await _artistRepository.DeleteEmptyArtists(libraryPath);
        await _searchIndex.RemoveItems(artistIds);

        _searchIndex.Commit();
        return Unit.Default;
    }

    private async Task<Either<BaseError, MediaItemScanResult<Artist>>> FindOrCreateArtist(
        int libraryPathId,
        string artistFolder)
    {
        ArtistMetadata metadata = await _localMetadataProvider.GetMetadataForArtist(artistFolder);
        Option<Artist> maybeArtist = await _artistRepository.GetArtistByMetadata(libraryPathId, metadata);
        return await maybeArtist.Match(
            artist => Right<BaseError, MediaItemScanResult<Artist>>(new MediaItemScanResult<Artist>(artist))
                .AsTask(),
            async () => await _artistRepository.AddArtist(libraryPathId, artistFolder, metadata));
    }

    private async Task<Either<BaseError, MediaItemScanResult<Artist>>> UpdateMetadataForArtist(
        MediaItemScanResult<Artist> result,
        string artistFolder)
    {
        try
        {
            Artist artist = result.Item;
            await LocateNfoFileForArtist(artistFolder).Match(
                async nfoFile =>
                {
                    bool shouldUpdate = Optional(artist.ArtistMetadata).Flatten().HeadOrNone().Match(
                        m => m.MetadataKind == MetadataKind.Fallback ||
                             m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
                        true);

                    if (shouldUpdate)
                    {
                        _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                        if (await _localMetadataProvider.RefreshSidecarMetadata(artist, nfoFile))
                        {
                            result.IsUpdated = true;
                        }
                    }
                },
                async () =>
                {
                    if (!Optional(artist.ArtistMetadata).Flatten().Any())
                    {
                        _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", artistFolder);
                        if (await _localMetadataProvider.RefreshFallbackMetadata(artist, artistFolder))
                        {
                            result.IsUpdated = true;
                        }
                    }
                });

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Artist>>> UpdateArtworkForArtist(
        MediaItemScanResult<Artist> result,
        string artistFolder,
        ArtworkKind artworkKind,
        CancellationToken cancellationToken)
    {
        try
        {
            Artist artist = result.Item;
            await LocateArtworkForArtist(artistFolder, artworkKind).IfSomeAsync(
                async artworkFile =>
                {
                    ArtistMetadata metadata = artist.ArtistMetadata.Head();
                    await RefreshArtwork(artworkFile, metadata, artworkKind, None, None, cancellationToken);
                });

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task ScanMusicVideos(
        LibraryPath libraryPath,
        string ffmpegPath,
        string ffprobePath,
        Artist artist,
        string artistFolder,
        CancellationToken cancellationToken)
    {
        var folderQueue = new Queue<string>();
        folderQueue.Enqueue(artistFolder);

        while (folderQueue.Count > 0)
        {
            string musicVideoFolder = folderQueue.Dequeue();
            // _logger.LogDebug("Scanning music video folder {Folder}", musicVideoFolder);

            var allFiles = _localFileSystem.ListFiles(musicVideoFolder)
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                .Filter(f => !Path.GetFileName(f).StartsWith("._"))
                .ToList();

            foreach (string subdirectory in _localFileSystem.ListSubdirectories(musicVideoFolder)
                         .OrderBy(identity))
            {
                folderQueue.Enqueue(subdirectory);
            }

            string etag = FolderEtag.Calculate(musicVideoFolder, _localFileSystem);
            Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                .Filter(f => f.Path == musicVideoFolder)
                .HeadOrNone();

            // skip folder if etag matches
            if (await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) == etag)
            {
                continue;
            }

            foreach (string file in allFiles.OrderBy(identity))
            {
                // TODO: figure out how to rebuild playouts
                Either<BaseError, MediaItemScanResult<MusicVideo>> maybeMusicVideo = await _musicVideoRepository
                    .GetOrAdd(artist, libraryPath, file)
                    .BindT(musicVideo => UpdateStatistics(musicVideo, ffmpegPath, ffprobePath))
                    .BindT(UpdateMetadata)
                    .BindT(result => UpdateThumbnail(result, cancellationToken))
                    .BindT(UpdateSubtitles)
                    .BindT(FlagNormal);

                await maybeMusicVideo.Match(
                    async result =>
                    {
                        if (result.IsAdded)
                        {
                            await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                        }
                        else if (result.IsUpdated)
                        {
                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                        }

                        await _libraryRepository.SetEtag(libraryPath, knownFolder, musicVideoFolder, etag);
                    },
                    error =>
                    {
                        _logger.LogWarning("Error processing music video at {Path}: {Error}", file, error.Value);
                        return Task.CompletedTask;
                    });
            }
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateMetadata(
        MediaItemScanResult<MusicVideo> result)
    {
        try
        {
            MusicVideo musicVideo = result.Item;
            await LocateNfoFile(musicVideo).Match(
                async nfoFile =>
                {
                    bool shouldUpdate = Optional(musicVideo.MusicVideoMetadata).Flatten().HeadOrNone().Match(
                        m => m.MetadataKind == MetadataKind.Fallback ||
                             m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
                        true);

                    if (shouldUpdate)
                    {
                        _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                        if (await _localMetadataProvider.RefreshSidecarMetadata(musicVideo, nfoFile))
                        {
                            result.IsUpdated = true;
                        }
                    }
                },
                async () =>
                {
                    if (!Optional(musicVideo.MusicVideoMetadata).Flatten().Any())
                    {
                        musicVideo.MusicVideoMetadata ??= new List<MusicVideoMetadata>();

                        string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
                        _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
                        if (await _localMetadataProvider.RefreshFallbackMetadata(musicVideo))
                        {
                            result.IsUpdated = true;
                        }
                    }
                });

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private Option<string> LocateNfoFileForArtist(string artistFolder) =>
        Optional(Path.Combine(artistFolder, "artist.nfo"))
            .Filter(s => _localFileSystem.FileExists(s));

    private Option<string> LocateArtworkForArtist(string artistFolder, ArtworkKind artworkKind)
    {
        string segment = artworkKind switch
        {
            ArtworkKind.Thumbnail => "thumb",
            ArtworkKind.FanArt => "fanart",
            _ => throw new ArgumentOutOfRangeException(nameof(artworkKind))
        };

        return ImageFileExtensions
            .Map(ext => $"{segment}.{ext}")
            .Map(f => Path.Combine(artistFolder, f))
            .Filter(s => _localFileSystem.FileExists(s))
            .HeadOrNone();
    }

    private Option<string> LocateNfoFile(MusicVideo musicVideo)
    {
        string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
        return Optional(Path.ChangeExtension(path, "nfo"))
            .Filter(s => _localFileSystem.FileExists(s))
            .HeadOrNone();
    }

    private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateThumbnail(
        MediaItemScanResult<MusicVideo> result,
        CancellationToken cancellationToken)
    {
        try
        {
            MusicVideo musicVideo = result.Item;
            await LocateThumbnail(musicVideo).IfSomeAsync(
                async thumbnailFile =>
                {
                    MusicVideoMetadata metadata = musicVideo.MusicVideoMetadata.Head();
                    await RefreshArtwork(thumbnailFile, metadata, ArtworkKind.Thumbnail, None, None, cancellationToken);
                });

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> UpdateSubtitles(
        MediaItemScanResult<MusicVideo> result)
    {
        try
        {
            await _localSubtitlesProvider.UpdateSubtitles(result.Item, None, true);
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private Option<string> LocateThumbnail(MusicVideo musicVideo)
    {
        string path = musicVideo.MediaVersions.Head().MediaFiles.Head().Path;
        return ImageFileExtensions
            .Map(ext => Path.ChangeExtension(path, ext))
            .Filter(f => _localFileSystem.FileExists(f))
            .HeadOrNone();
    }
}
