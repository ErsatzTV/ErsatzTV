using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Jellyfin
{
    public class JellyfinMovieLibraryScanner : IJellyfinMovieLibraryScanner
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger<JellyfinMovieLibraryScanner> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMediator _mediator;
        private readonly IMovieRepository _movieRepository;
        private readonly IJellyfinPathReplacementService _pathReplacementService;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;

        public JellyfinMovieLibraryScanner(
            IJellyfinApiClient jellyfinApiClient,
            ISearchIndex searchIndex,
            IMediator mediator,
            IMovieRepository movieRepository,
            ISearchRepository searchRepository,
            IJellyfinPathReplacementService pathReplacementService,
            IMediaSourceRepository mediaSourceRepository,
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            ILogger<JellyfinMovieLibraryScanner> logger)
        {
            _jellyfinApiClient = jellyfinApiClient;
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
            JellyfinLibrary library,
            string ffprobePath)
        {
            List<JellyfinItemEtag> existingMovies = await _movieRepository.GetExistingJellyfinMovies(library);

            // TODO: maybe get quick list of item ids and etags from api to compare first

            List<JellyfinPathReplacement> pathReplacements = await _mediaSourceRepository
                .GetJellyfinPathReplacements(library.MediaSourceId);

            Either<BaseError, List<JellyfinMovie>> maybeMovies = await _jellyfinApiClient.GetMovieLibraryItems(
                address,
                apiKey,
                library.MediaSourceId,
                library.ItemId);

            await maybeMovies.Match(
                async movies =>
                {
                    var validMovies = new List<JellyfinMovie>();
                    foreach (JellyfinMovie movie in movies)
                    {
                        string localPath = _pathReplacementService.GetReplacementJellyfinPath(
                            pathReplacements,
                            movie.MediaVersions.Head().MediaFiles.Head().Path,
                            false);

                        if (!_localFileSystem.FileExists(localPath))
                        {
                            _logger.LogWarning($"Skipping jellyfin movie that does not exist at {localPath}");
                        }
                        else
                        {
                            validMovies.Add(movie);
                        }
                    }

                    foreach (JellyfinMovie incoming in validMovies)
                    {
                        decimal percentCompletion = (decimal) validMovies.IndexOf(incoming) / validMovies.Count;
                        await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                        Option<JellyfinItemEtag> maybeExisting =
                            existingMovies.Find(ie => ie.ItemId == incoming.ItemId);

                        var updateStatistics = false;

                        await maybeExisting.Match(
                            async existing =>
                            {
                                if (existing.Etag == incoming.Etag)
                                {
                                    // _logger.LogDebug(
                                    //     $"NOOP: Etag has not changed for movie {incoming.MovieMetadata.Head().Title}");
                                    return;
                                }

                                _logger.LogDebug(
                                    $"UPDATE: Etag has changed for movie {incoming.MovieMetadata.Head().Title}");

                                updateStatistics = true;
                                incoming.LibraryPathId = library.Paths.Head().Id;
                                await _movieRepository.UpdateJellyfin(incoming);
                                await _searchIndex.UpdateItems(
                                    _searchRepository,
                                    new List<MediaItem> { incoming });
                            },
                            async () =>
                            {
                                // _logger.LogDebug(
                                //     $"INSERT: Item id is new for movie {incoming.MovieMetadata.Head().Title}");

                                updateStatistics = true;
                                incoming.LibraryPathId = library.Paths.Head().Id;
                                if (await _movieRepository.AddJellyfin(incoming))
                                {
                                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                                }
                            });

                        if (updateStatistics)
                        {
                            string localPath = _pathReplacementService.GetReplacementJellyfinPath(
                                pathReplacements,
                                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                                false);

                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                            Either<BaseError, bool> refreshResult =
                                await _localStatisticsProvider.RefreshStatistics(ffprobePath, incoming, localPath);

                            refreshResult.Match(
                                _ => { },
                                error => _logger.LogWarning(
                                    "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                                    "Statistics",
                                    localPath,
                                    error.Value));
                        }

                        // TODO: figure out how to rebuild playlists
                    }

                    var incomingMovieIds = validMovies.Map(s => s.ItemId).ToList();
                    var movieIds = existingMovies
                        .Filter(i => !incomingMovieIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    List<int> ids = await _movieRepository.RemoveMissingJellyfinMovies(library, movieIds);
                    await _searchIndex.RemoveItems(ids);

                    await _mediator.Publish(new LibraryScanProgress(library.Id, 0));
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing jellyfin library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Task.CompletedTask;
                });

            _searchIndex.Commit();
            return Unit.Default;
        }
    }
}
