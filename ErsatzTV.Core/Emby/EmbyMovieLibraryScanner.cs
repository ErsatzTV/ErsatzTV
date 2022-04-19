using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Emby;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Emby;

public class EmbyMovieLibraryScanner : IEmbyMovieLibraryScanner
{
    private readonly IEmbyApiClient _embyApiClient;
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
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
        ISearchRepository searchRepository,
        IEmbyPathReplacementService pathReplacementService,
        IMediaSourceRepository mediaSourceRepository,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILogger<EmbyMovieLibraryScanner> logger)
    {
        _embyApiClient = embyApiClient;
        _searchIndex = searchIndex;
        _mediator = mediator;
        _movieRepository = movieRepository;
        _searchRepository = searchRepository;
        _pathReplacementService = pathReplacementService;
        _mediaSourceRepository = mediaSourceRepository;
        _localFileSystem = localFileSystem;
        _localStatisticsProvider = localStatisticsProvider;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        string address,
        string apiKey,
        EmbyLibrary library,
        string ffmpegPath,
        string ffprobePath)
    {
        List<EmbyItemEtag> existingMovies = await _movieRepository.GetExistingEmbyMovies(library);

        // TODO: maybe get quick list of item ids and etags from api to compare first
        // TODO: paging?

        List<EmbyPathReplacement> pathReplacements = await _mediaSourceRepository
            .GetEmbyPathReplacements(library.MediaSourceId);

        Either<BaseError, List<EmbyMovie>> maybeMovies = await _embyApiClient.GetMovieLibraryItems(
            address,
            apiKey,
            library.MediaSourceId,
            library.ItemId);

        await maybeMovies.Match(
            async movies =>
            {
                var validMovies = new List<EmbyMovie>();
                foreach (EmbyMovie movie in movies.OrderBy(m => m.MovieMetadata.Head().Title))
                {
                    string localPath = _pathReplacementService.GetReplacementEmbyPath(
                        pathReplacements,
                        movie.MediaVersions.Head().MediaFiles.Head().Path,
                        false);

                    if (!_localFileSystem.FileExists(localPath))
                    {
                        _logger.LogWarning("Skipping emby movie that does not exist at {Path}", localPath);
                    }
                    else
                    {
                        validMovies.Add(movie);
                    }
                }

                foreach (EmbyMovie incoming in validMovies)
                {
                    EmbyMovie incomingMovie = incoming;

                    decimal percentCompletion = (decimal)validMovies.IndexOf(incoming) / validMovies.Count;
                    await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                    Option<EmbyItemEtag> maybeExisting =
                        existingMovies.Find(ie => ie.ItemId == incoming.ItemId);

                    var updateStatistics = false;

                    await maybeExisting.Match(
                        async existing =>
                        {
                            try
                            {
                                if (existing.Etag == incoming.Etag)
                                {
                                    // _logger.LogDebug(
                                    //     $"NOOP: Etag has not changed for movie {incoming.MovieMetadata.Head().Title}");
                                    return;
                                }

                                _logger.LogDebug(
                                    "UPDATE: Etag has changed for movie {Movie}",
                                    incoming.MovieMetadata.Head().Title);

                                updateStatistics = true;
                                incoming.LibraryPathId = library.Paths.Head().Id;
                                Option<EmbyMovie> maybeUpdated = await _movieRepository.UpdateEmby(incoming);
                                foreach (EmbyMovie updated in maybeUpdated)
                                {
                                    await _searchIndex.UpdateItems(
                                        _searchRepository,
                                        new List<MediaItem> { updated });

                                    incomingMovie = updated;
                                }
                            }
                            catch (Exception ex)
                            {
                                updateStatistics = false;
                                _logger.LogError(
                                    ex,
                                    "Error updating movie {Movie}",
                                    incoming.MovieMetadata.Head().Title);
                            }
                        },
                        async () =>
                        {
                            try
                            {
                                // _logger.LogDebug(
                                //     $"INSERT: Item id is new for movie {incoming.MovieMetadata.Head().Title}");

                                updateStatistics = true;
                                incoming.LibraryPathId = library.Paths.Head().Id;
                                if (await _movieRepository.AddEmby(incoming))
                                {
                                    await _searchIndex.AddItems(
                                        _searchRepository,
                                        new List<MediaItem> { incoming });
                                }
                            }
                            catch (Exception ex)
                            {
                                updateStatistics = false;
                                _logger.LogError(
                                    ex,
                                    "Error adding movie {Movie}",
                                    incoming.MovieMetadata.Head().Title);
                            }
                        });

                    if (updateStatistics)
                    {
                        string localPath = _pathReplacementService.GetReplacementEmbyPath(
                            pathReplacements,
                            incoming.MediaVersions.Head().MediaFiles.Head().Path,
                            false);

                        _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                        Either<BaseError, bool> refreshResult =
                            await _localStatisticsProvider.RefreshStatistics(
                                ffmpegPath,
                                ffprobePath,
                                incomingMovie,
                                localPath);

                        await refreshResult.Match(
                            async _ =>
                            {
                                Option<MediaItem> updated = await _searchRepository.GetItemToIndex(incomingMovie.Id);
                                if (updated.IsSome)
                                {
                                    await _searchIndex.UpdateItems(
                                        _searchRepository,
                                        new List<MediaItem> { updated.ValueUnsafe() });
                                }
                            },
                            error =>
                            {
                                _logger.LogWarning(
                                    "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                                    "Statistics",
                                    localPath,
                                    error.Value);

                                return Task.CompletedTask;
                            });
                    }

                    // TODO: figure out how to rebuild playlists
                }

                var incomingMovieIds = validMovies.Map(s => s.ItemId).ToList();
                var movieIds = existingMovies
                    .Filter(i => !incomingMovieIds.Contains(i.ItemId))
                    .Map(m => m.ItemId)
                    .ToList();
                List<int> ids = await _movieRepository.RemoveMissingEmbyMovies(library, movieIds);
                await _searchIndex.RemoveItems(ids);

                await _mediator.Publish(new LibraryScanProgress(library.Id, 0));
                _searchIndex.Commit();
            },
            error =>
            {
                _logger.LogWarning(
                    "Error synchronizing emby library {Path}: {Error}",
                    library.Name,
                    error.Value);

                return Task.CompletedTask;
            });

        _searchIndex.Commit();
        return Unit.Default;
    }
}
