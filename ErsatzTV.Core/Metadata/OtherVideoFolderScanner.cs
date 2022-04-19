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

namespace ErsatzTV.Core.Metadata;

public class OtherVideoFolderScanner : LocalFolderScanner, IOtherVideoFolderScanner
{
    private readonly IClient _client;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalMetadataProvider _localMetadataProvider;
    private readonly ILogger<OtherVideoFolderScanner> _logger;
    private readonly IMediator _mediator;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IOtherVideoRepository _otherVideoRepository;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public OtherVideoFolderScanner(
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalMetadataProvider localMetadataProvider,
        IMetadataRepository metadataRepository,
        IImageCache imageCache,
        IMediator mediator,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IOtherVideoRepository otherVideoRepository,
        ILibraryRepository libraryRepository,
        IMediaItemRepository mediaItemRepository,
        IFFmpegProcessService ffmpegProcessService,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<OtherVideoFolderScanner> logger) : base(
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
        _metadataRepository = metadataRepository;
        _mediator = mediator;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _otherVideoRepository = otherVideoRepository;
        _libraryRepository = libraryRepository;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanFolder(
        LibraryPath libraryPath,
        string ffmpegPath,
        string ffprobePath,
        decimal progressMin,
        decimal progressMax)
    {
        decimal progressSpread = progressMax - progressMin;

        var foldersCompleted = 0;

        var folderQueue = new Queue<string>();

        if (ShouldIncludeFolder(libraryPath.Path))
        {
            folderQueue.Enqueue(libraryPath.Path);
        }

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

            string otherVideoFolder = folderQueue.Dequeue();
            foldersCompleted++;

            var filesForEtag = _localFileSystem.ListFiles(otherVideoFolder).ToList();

            var allFiles = filesForEtag
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                .Filter(f => !Path.GetFileName(f).StartsWith("._"))
                .ToList();

            foreach (string subdirectory in _localFileSystem.ListSubdirectories(otherVideoFolder)
                         .Filter(ShouldIncludeFolder)
                         .OrderBy(identity))
            {
                folderQueue.Enqueue(subdirectory);
            }

            string etag = FolderEtag.Calculate(otherVideoFolder, _localFileSystem);
            Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                .Filter(f => f.Path == otherVideoFolder)
                .HeadOrNone();

            // skip folder if etag matches
            if (!allFiles.Any() || await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) == etag)
            {
                continue;
            }

            _logger.LogDebug(
                "UPDATE: Etag has changed for folder {Folder}",
                otherVideoFolder);

            foreach (string file in allFiles.OrderBy(identity))
            {
                Either<BaseError, MediaItemScanResult<OtherVideo>> maybeVideo = await _otherVideoRepository
                    .GetOrAdd(libraryPath, file)
                    .BindT(video => UpdateStatistics(video, ffmpegPath, ffprobePath))
                    .BindT(UpdateMetadata)
                    .BindT(UpdateSubtitles)
                    .BindT(FlagNormal);

                await maybeVideo.Match(
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

                        await _libraryRepository.SetEtag(libraryPath, knownFolder, otherVideoFolder, etag);
                    },
                    error =>
                    {
                        _logger.LogWarning("Error processing other video at {Path}: {Error}", file, error.Value);
                        return Task.CompletedTask;
                    });
            }
        }

        foreach (string path in await _otherVideoRepository.FindOtherVideoPaths(libraryPath))
        {
            if (!_localFileSystem.FileExists(path))
            {
                _logger.LogInformation("Flagging missing other video at {Path}", path);
                List<int> otherVideoIds = await FlagFileNotFound(libraryPath, path);
                await _searchIndex.RebuildItems(_searchRepository, otherVideoIds);
            }
            else if (Path.GetFileName(path).StartsWith("._"))
            {
                _logger.LogInformation("Removing dot underscore file at {Path}", path);
                List<int> otherVideoIds = await _otherVideoRepository.DeleteByPath(libraryPath, path);
                await _searchIndex.RemoveItems(otherVideoIds);
            }
        }

        await _libraryRepository.CleanEtagsForLibraryPath(libraryPath);

        _searchIndex.Commit();
        return Unit.Default;
    }

    private async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> UpdateMetadata(
        MediaItemScanResult<OtherVideo> result)
    {
        try
        {
            OtherVideo otherVideo = result.Item;
            if (!Optional(otherVideo.OtherVideoMetadata).Flatten().Any())
            {
                otherVideo.OtherVideoMetadata ??= new List<OtherVideoMetadata>();

                string path = otherVideo.MediaVersions.Head().MediaFiles.Head().Path;
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
                if (await _localMetadataProvider.RefreshFallbackMetadata(otherVideo))
                {
                    result.IsUpdated = true;
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

    private async Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> UpdateSubtitles(
        MediaItemScanResult<OtherVideo> result)
    {
        try
        {
            OtherVideo otherVideo = result.Item;

            foreach (OtherVideoMetadata metadata in otherVideo.OtherVideoMetadata)
            {
                MediaVersion version = otherVideo.GetHeadVersion();
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
}
