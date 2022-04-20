﻿using System.Globalization;
using Bugsnag;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using MediatR;
using Microsoft.Extensions.Logging;
using Seq = LanguageExt.Seq;

namespace ErsatzTV.Core.Metadata;

public class MovieFolderScanner : LocalFolderScanner, IMovieFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly IMediaItemRepository _mediaItemRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILogger<MovieFolderScanner> _logger;
    private readonly IMediator _mediator;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public MovieFolderScanner(
        ILocalFileSystem localFileSystem,
        IMovieRepository movieRepository,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILocalMetadataProvider localMetadataProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IMediator mediator,
        IFFmpegProcessService ffmpegProcessService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<MovieFolderScanner> logger)
        : base(
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
        _movieRepository = movieRepository;
        _localSubtitlesProvider = localSubtitlesProvider;
        _localMetadataProvider = localMetadataProvider;
        _metadataRepository = metadataRepository;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _libraryRepository = libraryRepository;
        _mediaItemRepository = mediaItemRepository;
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
        List<CultureInfo> languageCodes = await _mediaItemRepository.GetAllLanguageCodeCultures();
        
        decimal progressSpread = progressMax - progressMin;

        var foldersCompleted = 0;

        var folderQueue = new Queue<string>();
        foreach (string folder in _localFileSystem.ListSubdirectories(libraryPath.Path)
                     .Filter(ShouldIncludeFolder)
                     .OrderBy(identity))
        {
            folderQueue.Enqueue(folder);
        }

        while (folderQueue.Count > 0)
        {
            decimal percentCompletion = (decimal)foldersCompleted / (foldersCompleted + folderQueue.Count);
            await _mediator.Publish(
                new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread));

            string movieFolder = folderQueue.Dequeue();
            foldersCompleted++;

            var filesForEtag = _localFileSystem.ListFiles(movieFolder).ToList();

            var allFiles = filesForEtag
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                .Filter(f => !Path.GetFileName(f).StartsWith("._"))
                .Filter(
                    f => !ExtraFiles.Any(
                        e => Path.GetFileNameWithoutExtension(f).EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (allFiles.Count == 0)
            {
                foreach (string subdirectory in _localFileSystem.ListSubdirectories(movieFolder)
                             .Filter(ShouldIncludeFolder)
                             .OrderBy(identity))
                {
                    folderQueue.Enqueue(subdirectory);
                }

                continue;
            }

            string etag = FolderEtag.Calculate(movieFolder, _localFileSystem);
            Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                .Filter(f => f.Path == movieFolder)
                .HeadOrNone();

            // skip folder if etag matches
            if (await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) == etag)
            {
                continue;
            }

            _logger.LogDebug(
                "UPDATE: Etag has changed for folder {Folder}",
                movieFolder);

            foreach (string file in allFiles.OrderBy(identity))
            {
                // TODO: figure out how to rebuild playlists
                Either<BaseError, MediaItemScanResult<Movie>> maybeMovie = await _movieRepository
                    .GetOrAdd(libraryPath, file)
                    .BindT(movie => UpdateStatistics(movie, ffmpegPath, ffprobePath))
                    .BindT(UpdateMetadata)
                    .BindT(movie => UpdateArtwork(movie, ArtworkKind.Poster, cancellationToken))
                    .BindT(movie => UpdateArtwork(movie, ArtworkKind.FanArt, cancellationToken))
                    .BindT(movie => UpdateSubtitles(languageCodes, movie, cancellationToken))
                    .BindT(FlagNormal);

                await maybeMovie.Match(
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

                        await _libraryRepository.SetEtag(libraryPath, knownFolder, movieFolder, etag);
                    },
                    error =>
                    {
                        _logger.LogWarning("Error processing movie at {Path}: {Error}", file, error.Value);
                        return Task.CompletedTask;
                    });
            }
        }

        foreach (string path in await _movieRepository.FindMoviePaths(libraryPath))
        {
            if (!_localFileSystem.FileExists(path))
            {
                _logger.LogInformation("Flagging missing movie at {Path}", path);
                List<int> ids = await FlagFileNotFound(libraryPath, path);
                await _searchIndex.RebuildItems(_searchRepository, ids);
            }
            else if (Path.GetFileName(path).StartsWith("._"))
            {
                _logger.LogInformation("Removing dot underscore file at {Path}", path);
                List<int> ids = await _movieRepository.DeleteByPath(libraryPath, path);
                await _searchIndex.RemoveItems(ids);
            }
        }

        await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

        _searchIndex.Commit();
        return Unit.Default;
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateMetadata(
        MediaItemScanResult<Movie> result)
    {
        try
        {
            Movie movie = result.Item;
            await LocateNfoFile(movie).Match(
                async nfoFile =>
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
                },
                async () =>
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
                });

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
            await LocateArtwork(movie, artworkKind).IfSomeAsync(
                async posterFile =>
                {
                    MovieMetadata metadata = movie.MovieMetadata.Head();
                    await RefreshArtwork(posterFile, metadata, artworkKind, None, None, cancellationToken);
                });

            return result;
        }
        catch (Exception ex)
        {
            _client.Notify(ex);
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<Movie>>> UpdateSubtitles(
        List<CultureInfo> languageCodes,
        MediaItemScanResult<Movie> result,
        CancellationToken _)
    {
        try
        {
            Movie movie = result.Item;

            foreach (MovieMetadata metadata in movie.MovieMetadata)
            {
                MediaVersion version = movie.GetHeadVersion();
                var subtitleStreams = version.Streams
                    .Filter(s => s.MediaStreamKind == MediaStreamKind.Subtitle)
                    .ToList();

                var subtitles = new List<Subtitle>();

                foreach (MediaStream stream in subtitleStreams)
                {
                    var subtitle = new Subtitle
                    {
                        Codec = stream.Codec,
                        Default = stream.Default,
                        Forced = stream.Forced,
                        Language = stream.Language,
                        StreamIndex = stream.Index,
                        SubtitleKind = SubtitleKind.Embedded,
                        DateAdded = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow
                    };

                    subtitles.Add(subtitle);
                }

                subtitles.AddRange(_localSubtitlesProvider.LocateExternalSubtitles(languageCodes, movie));

                await _metadataRepository.UpdateSubtitles(metadata, subtitles);
            }

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
        IEnumerable<string> possibleMoviePosters = ImageFileExtensions.Collect(
                ext => new[] { $"{segment}.{ext}", Path.GetFileNameWithoutExtension(path) + $"-{segment}.{ext}" })
            .Map(f => Path.Combine(folder, f));
        Option<string> result = possibleMoviePosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
        if (result.IsNone && artworkKind == ArtworkKind.Poster)
        {
            IEnumerable<string> possibleFolderPosters = ImageFileExtensions.Collect(
                    ext => new[] { $"folder.{ext}" })
                .Map(f => Path.Combine(folder, f));
            result = possibleFolderPosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
        }

        return result;
    }
}
