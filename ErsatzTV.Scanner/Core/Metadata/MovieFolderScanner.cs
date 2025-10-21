using System.Collections.Immutable;
using Bugsnag;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Scanner.Core.Interfaces;
using ErsatzTV.Scanner.Core.Interfaces.FFmpeg;
using ErsatzTV.Scanner.Core.Interfaces.Metadata;
using Microsoft.Extensions.Logging;
using Seq = LanguageExt.Seq;

namespace ErsatzTV.Scanner.Core.Metadata;

public class MovieFolderScanner : LocalFolderScanner, IMovieFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalChaptersProvider _localChaptersProvider;
    private readonly IScannerProxy _scannerProxy;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<MovieFolderScanner> _logger;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly IMovieRepository _movieRepository;

    public MovieFolderScanner(
        IScannerProxy scannerProxy,
        ILocalFileSystem localFileSystem,
        IMovieRepository movieRepository,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalChaptersProvider localChaptersProvider,
        ILocalMetadataProvider localMetadataProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IFFmpegPngService ffmpegPngService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<MovieFolderScanner> logger)
        : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            mediaItemRepository,
            imageCache,
            ffmpegPngService,
            tempFilePool,
            client,
            logger)
    {
        _scannerProxy = scannerProxy;
        _localFileSystem = localFileSystem;
        _movieRepository = movieRepository;
        _localSubtitlesProvider = localSubtitlesProvider;
        _localChaptersProvider = localChaptersProvider;
        _localMetadataProvider = localMetadataProvider;
        _libraryRepository = libraryRepository;
        _mediaItemRepository = mediaItemRepository;
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
        try
        {
            ImmutableHashSet<string> allTrashedItems = await _mediaItemRepository.GetAllTrashedItems(libraryPath);

            decimal progressSpread = progressMax - progressMin;

            var foldersCompleted = 0;

            var folderQueue = new Queue<string>();

            string normalizedLibraryPath = libraryPath.Path.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            if (libraryPath.Path != normalizedLibraryPath)
            {
                await _libraryRepository.UpdatePath(libraryPath, normalizedLibraryPath);
            }

            foreach (string folder in _localFileSystem.ListSubdirectories(libraryPath.Path)
                         .Filter(ShouldIncludeFolder)
                         .OrderBy(identity))
            {
                folderQueue.Enqueue(folder);
            }

            while (folderQueue.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ScanCanceled();
                }

                decimal percentCompletion = (decimal)foldersCompleted / (foldersCompleted + folderQueue.Count);
                if (!await _scannerProxy.UpdateProgress(
                        progressMin + percentCompletion * progressSpread,
                        cancellationToken))
                {
                    return new ScanCanceled();
                }

                string movieFolder = folderQueue.Dequeue();
                Option<int> maybeParentFolder = await _libraryRepository.GetParentFolderId(
                    libraryPath,
                    movieFolder,
                    cancellationToken);
                foldersCompleted++;

                var filesForEtag = _localFileSystem.ListFiles(movieFolder).ToList();

                var allFiles = filesForEtag
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(f => !Path.GetFileName(f).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                    .Filter(f => !ExtraFiles.Any(e =>
                        Path.GetFileNameWithoutExtension(f).EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                string etag = FolderEtag.Calculate(movieFolder, _localFileSystem);
                LibraryFolder knownFolder = await _libraryRepository.GetOrAddFolder(
                    libraryPath,
                    maybeParentFolder,
                    movieFolder);

                if (allFiles.Count == 0)
                {
                    foreach (string subdirectory in _localFileSystem.ListSubdirectories(movieFolder)
                                 .Filter(ShouldIncludeFolder)
                                 .OrderBy(identity))
                    {
                        folderQueue.Enqueue(subdirectory);
                    }

                    // store etag for now-empty folders
                    await _libraryRepository.SetEtag(libraryPath, knownFolder, movieFolder, etag);

                    continue;
                }

                if (knownFolder.Etag == etag)
                {
                    if (allFiles.Any(allTrashedItems.Contains))
                    {
                        _logger.LogDebug("Previously trashed items are now present in folder {Folder}", movieFolder);
                    }
                    else
                    {
                        // etag matches and no trashed items are now present, continue to next folder
                        continue;
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "UPDATE: Etag has changed for folder {Folder}",
                        movieFolder);
                }

                foreach (string file in allFiles.OrderBy(identity))
                {
                    // TODO: figure out how to rebuild playlists
                    Either<BaseError, MediaItemScanResult<Movie>> maybeMovie = await _movieRepository
                        .GetOrAdd(libraryPath, knownFolder, file, cancellationToken)
                        .BindT(movie => UpdateStatistics(movie, ffmpegPath, ffprobePath))
                        .BindT(video => UpdateLibraryFolderId(video, knownFolder))
                        .BindT(UpdateMetadata)
                        .BindT(movie => UpdateArtwork(movie, ArtworkKind.Poster, cancellationToken))
                        .BindT(movie => UpdateArtwork(movie, ArtworkKind.FanArt, cancellationToken))
                        .BindT(movie => UpdateSubtitles(movie, cancellationToken))
                        .BindT(movie => UpdateChapters(movie, cancellationToken))
                        .BindT(FlagNormal);

                    foreach (BaseError error in maybeMovie.LeftToSeq())
                    {
                        _logger.LogWarning("Error processing movie at {Path}: {Error}", file, error.Value);
                    }

                    foreach (MediaItemScanResult<Movie> result in maybeMovie.RightToSeq())
                    {
                        if (result.IsAdded || result.IsUpdated)
                        {
                            if (!await _scannerProxy.ReindexMediaItems([result.Item.Id], cancellationToken))
                            {
                                _logger.LogWarning("Failed to reindex media items from scanner process");
                            }
                        }

                        await _libraryRepository.SetEtag(libraryPath, knownFolder, movieFolder, etag);
                    }
                }
            }

            foreach (string path in await _movieRepository.FindMoviePaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Flagging missing movie at {Path}", path);
                    List<int> ids = await FlagFileNotFound(libraryPath, path);
                    if (!await _scannerProxy.ReindexMediaItems(ids.ToArray(), cancellationToken))
                    {
                        _logger.LogWarning("Failed to reindex media items from scanner process");
                    }

                }
                else if (Path.GetFileName(path).StartsWith("._", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    List<int> ids = await _movieRepository.DeleteByPath(libraryPath, path);
                    if (!await _scannerProxy.RemoveMediaItems(ids.ToArray(), cancellationToken))
                    {
                        _logger.LogWarning("Failed to remove media items from scanner process");
                    }
                }
            }

            await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);
            return Unit.Default;
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateLibraryFolderId(
        MediaItemScanResult<Movie> video,
        LibraryFolder libraryFolder)
    {
        MediaFile mediaFile = video.Item.GetHeadVersion().MediaFiles.Head();
        if (mediaFile.LibraryFolderId != libraryFolder.Id)
        {
            await _libraryRepository.UpdateLibraryFolderId(mediaFile, libraryFolder.Id);
            video.IsUpdated = true;
        }

        return video;
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateMetadata(
        MediaItemScanResult<Movie> result)
    {
        try
        {
            Movie movie = result.Item;

            Option<string> maybeNfoFile = LocateNfoFile(movie);
            if (maybeNfoFile.IsNone)
            {
                if (!Optional(movie.MovieMetadata).Flatten().Any())
                {
                    string path = movie.MediaVersions.Head().MediaFiles.Head().Path;
                    _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
                    if (await _localMetadataProvider.RefreshFallbackMetadata(movie))
                    {
                        result.IsUpdated = true;
                    }
                }
            }

            foreach (string nfoFile in maybeNfoFile)
            {
                bool shouldUpdate = Optional(movie.MovieMetadata).Flatten().HeadOrNone().Match(
                    m => m.MetadataKind == MetadataKind.Fallback ||
                         m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
                    true);

                if (shouldUpdate)
                {
                    _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                    if (await _localMetadataProvider.RefreshSidecarMetadata(movie, nfoFile))
                    {
                        result.IsUpdated = true;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateArtwork(
        MediaItemScanResult<Movie> result,
        ArtworkKind artworkKind,
        CancellationToken cancellationToken)
    {
        try
        {
            Movie movie = result.Item;
            Option<string> maybeArtwork = LocateArtwork(movie, artworkKind);
            foreach (string posterFile in maybeArtwork)
            {
                MovieMetadata metadata = movie.MovieMetadata.Head();
                await RefreshArtwork(posterFile, metadata, artworkKind, None, None, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateSubtitles(
        MediaItemScanResult<Movie> result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _localSubtitlesProvider.UpdateSubtitles(result.Item, None, true, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateChapters(
        MediaItemScanResult<Movie> result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _localChaptersProvider.UpdateChapters(result.Item, None, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private Option<string> LocateNfoFile(Movie movie)
    {
        string path = movie.MediaVersions.Head().MediaFiles.Head().Path;
        string movieAsNfo = Path.ChangeExtension(path, "nfo");
        string movieNfo = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "movie.nfo");
        return Seq.create(movieAsNfo, movieNfo)
            .Filter(s => _localFileSystem.FileExists(s))
            .HeadOrNone();
    }

    private Option<string> LocateArtwork(Movie movie, ArtworkKind artworkKind)
    {
        string segment = artworkKind switch
        {
            ArtworkKind.Poster => "poster",
            ArtworkKind.FanArt => "fanart",
            _ => throw new ArgumentOutOfRangeException(nameof(artworkKind))
        };

        string path = movie.MediaVersions.Head().MediaFiles.Head().Path;
        string folder = Path.GetDirectoryName(path) ?? string.Empty;
        IEnumerable<string> possibleMoviePosters = ImageFileExtensions.Collect(ext =>
                new[] { $"{segment}.{ext}", Path.GetFileNameWithoutExtension(path) + $"-{segment}.{ext}" })
            .Map(f => Path.Combine(folder, f));
        Option<string> result = possibleMoviePosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
        if (result.IsNone && artworkKind == ArtworkKind.Poster)
        {
            IEnumerable<string> possibleFolderPosters = ImageFileExtensions.Collect(ext => new[] { $"folder.{ext}" })
                .Map(f => Path.Combine(folder, f));
            result = possibleFolderPosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
        }

        return result;
    }
}
