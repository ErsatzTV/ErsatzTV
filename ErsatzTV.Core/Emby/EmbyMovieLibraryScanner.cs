using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyMovieLibraryScanner : IEmbyMovieLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly IEmbyMovieRepository _embyMovieRepository;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<EmbyMovieLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IMovieRepository _movieRepository;
    private readonly IEmbyPathReplacementService _pathReplacementService;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public EmbyMovieLibraryScanner(
        IEmbyApiClient embyApiClient,
        ISearchIndex searchIndex,
        IMediator mediator,
        IMovieRepository movieRepository,
        IEmbyMovieRepository embyMovieRepository,
        ISearchRepository searchRepository,
        IEmbyPathReplacementService pathReplacementService,
        IMediaSourceRepository mediaSourceRepository,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<EmbyMovieLibraryScanner> logger)
    {
        _embyApiClient = embyApiClient;
        _searchIndex = searchIndex;
        _mediator = mediator;
        _movieRepository = movieRepository;
        _embyMovieRepository = embyMovieRepository;
        _searchRepository = searchRepository;
        _pathReplacementService = pathReplacementService;
        _mediaSourceRepository = mediaSourceRepository;
        _localFileSystem = localFileSystem;
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        try
        {
            Either<BaseError, List<EmbyMovie>> entries = await _embyApiClient.GetMovieLibraryItems(
                address,
                apiKey,
                library.ItemId);

            foreach (BaseError error in entries.LeftToSeq())
            {
                return error;
            }

            return await ScanLibrary(
                library,
                ffmpegPath,
                ffprobePath,
                entries.RightToSeq().Flatten().ToList(),
                cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
        finally
        {
            _searchIndex.Commit();
        }
    }

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath,
        List<EmbyMovie> movieEntries,
        CancellationToken cancellationToken)
    {
        List<EmbyItemEtag> existingMovies = await _movieRepository.GetExistingEmbyMovies(library);

        // TODO: maybe get quick list of item ids and etags from api to compare first
        // TODO: paging?

        List<EmbyPathReplacement> pathReplacements = await _mediaSourceRepository
            .GetEmbyPathReplacements(library.MediaSourceId);

        foreach (EmbyMovie incoming in movieEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)movieEntries.IndexOf(incoming) / movieEntries.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            if (await ShouldScanItem(library, pathReplacements, existingMovies, incoming) == false)
            {
                continue;
            }

            // TODO: figure out how to rebuild playlists
            Either<BaseError, MediaItemScanResult<EmbyMovie>> maybeMovie = await _embyMovieRepository
                .GetOrAdd(library, incoming)
                .BindT(existing => UpdateStatistics(pathReplacements, existing, incoming, ffmpegPath, ffprobePath))
                .BindT(existing => UpdateSubtitles(pathReplacements, existing, incoming));

            foreach (MediaItemScanResult<EmbyMovie> result in maybeMovie.RightToSeq())
            {
                await _embyMovieRepository.SetEtag(result.Item, incoming.Etag);

                string plexPath = incoming.MediaVersions.Head().MediaFiles.Head().Path;

                string localPath = _pathReplacementService.GetReplacementEmbyPath(
                    pathReplacements,
                    plexPath,
                    false);

                if (_localFileSystem.FileExists(localPath))
                {
                    await _embyMovieRepository.FlagNormal(library, result.Item);
                    result.IsUpdated = true;
                }
                else
                {
                    await _embyMovieRepository.FlagUnavailable(library, result.Item);
                }

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash items that are no longer present on the media server
        var fileNotFoundItemIds = existingMovies.Map(m => m.ItemId).Except(movieEntries.Map(m => m.ItemId)).ToList();
        List<int> ids = await _embyMovieRepository.FlagFileNotFound(library, fileNotFoundItemIds);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);

        return Unit.Default;
    }

    private async Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> UpdateStatistics(
        List<EmbyPathReplacement> pathReplacements,
        MediaItemScanResult<EmbyMovie> result,
        EmbyMovie incoming,
        string ffmpegPath,
        string ffprobePath)
    {
        EmbyMovie existing = result.Item;

        if (result.IsAdded || existing.Etag != incoming.Etag || existing.MediaVersions.Head().Streams.Count == 0)
        {
            string localPath = _pathReplacementService.GetReplacementEmbyPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            // only refresh statistics if the file exists
            if (_localFileSystem.FileExists(localPath))
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(
                        ffmpegPath,
                        ffprobePath,
                        existing,
                        localPath);

                foreach (BaseError error in refreshResult.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                        "Statistics",
                        localPath,
                        error.Value);
                }

                foreach (bool _ in refreshResult.RightToSeq())
                {
                    result.IsUpdated = true;
                }
            }
        }

        return result;
    }

    private async Task<bool> ShouldScanItem(
        EmbyLibrary library,
        List<EmbyPathReplacement> pathReplacements,
        List<EmbyItemEtag> existingMovies,
        EmbyMovie incoming)
    {
        Option<EmbyItemEtag> maybeExisting = existingMovies.Find(ie => ie.ItemId == incoming.ItemId);
        string existingEtag = await maybeExisting
            .Map(e => e.Etag ?? string.Empty)
            .IfNoneAsync(string.Empty);
        MediaItemState existingState = await maybeExisting
            .Map(e => e.State)
            .IfNoneAsync(MediaItemState.Normal);

        string embyPath = incoming.MediaVersions.Head().MediaFiles.Head().Path;

        string localPath = _pathReplacementService.GetReplacementEmbyPath(pathReplacements, embyPath, false);

        // if media is unavailable, only scan if file now exists
        if (existingState == MediaItemState.Unavailable)
        {
            if (!_localFileSystem.FileExists(localPath))
            {
                return false;
            }
        }
        else if (existingEtag == incoming.Etag)
        {
            if (!_localFileSystem.FileExists(localPath))
            {
                foreach (int id in await _embyMovieRepository.FlagUnavailable(library, incoming))
                {
                    await _searchIndex.RebuildItems(_searchRepository, new List<int> { id });
                }
            }

            // _logger.LogDebug("NOOP: etag has not changed for emby movie with item id {ItemId}", incoming.ItemId);
            return false;
        }

        _logger.LogDebug(
            "UPDATE: Etag has changed for movie {Movie}",
            incoming.MovieMetadata.Head().Title);

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<EmbyMovie>>> UpdateSubtitles(
        List<EmbyPathReplacement> pathReplacements,
        MediaItemScanResult<EmbyMovie> result,
        EmbyMovie incoming)
    {
        try
        {
            string localPath = _pathReplacementService.GetReplacementEmbyPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            await _localSubtitlesProvider.UpdateSubtitles(result.Item, localPath, false);

            return result;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
