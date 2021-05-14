using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
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
        private readonly ILogger<JellyfinMovieLibraryScanner> _logger;
        private readonly IMediator _mediator;
        private readonly IMovieRepository _movieRepository;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;

        public JellyfinMovieLibraryScanner(
            IJellyfinApiClient jellyfinApiClient,
            ISearchIndex searchIndex,
            IMediator mediator,
            IMovieRepository movieRepository,
            ISearchRepository searchRepository,
            ILogger<JellyfinMovieLibraryScanner> logger)
        {
            _jellyfinApiClient = jellyfinApiClient;
            _searchIndex = searchIndex;
            _mediator = mediator;
            _movieRepository = movieRepository;
            _searchRepository = searchRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(string address, string apiKey, JellyfinLibrary library)
        {
            List<JellyfinItemEtag> existingMovies = await _movieRepository.GetExistingJellyfinMovies(library);

            // TODO: maybe get quick list of item ids and etags from api to compare first

            Either<BaseError, List<JellyfinMovie>> maybeMovies = await _jellyfinApiClient.GetMovieLibraryItems(
                address,
                apiKey,
                library.MediaSourceId,
                library.ItemId);

            await maybeMovies.Match(
                async movies =>
                {
                    foreach (JellyfinMovie incoming in movies)
                    {
                        decimal percentCompletion = (decimal) movies.IndexOf(incoming) / movies.Count;
                        await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                        Option<JellyfinItemEtag> maybeExisting =
                            existingMovies.Find(ie => ie.ItemId == incoming.ItemId);
                        await maybeExisting.Match(
                            async existing =>
                            {
                                if (existing.Etag == incoming.Etag)
                                {
                                    _logger.LogDebug(
                                        $"NOOP: Etag has not changed for movie {incoming.MovieMetadata.Head().Title}");
                                    return;
                                }

                                _logger.LogDebug(
                                    $"UPDATE: Etag has changed for movie {incoming.MovieMetadata.Head().Title}");

                                incoming.LibraryPathId = library.Paths.Head().Id;
                                await _movieRepository.UpdateJellyfin(incoming);
                                await _searchIndex.UpdateItems(
                                    _searchRepository,
                                    new List<MediaItem> { incoming });
                            },
                            async () =>
                            {
                                _logger.LogDebug(
                                    $"INSERT: Item id is new for movie {incoming.MovieMetadata.Head().Title}");

                                incoming.LibraryPathId = library.Paths.Head().Id;
                                if (await _movieRepository.AddJellyfin(incoming))
                                {
                                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                                }
                            });

                        // TODO: figure out how to rebuild playlists
                        // Either<BaseError, MediaItemScanResult<PlexMovie>> maybeMovie = await _movieRepository
                        //     .GetOrAdd(library, incoming)
                        //     .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                        //     .BindT(existing => UpdateMetadata(existing, incoming, library, connection, token))
                        //     .BindT(existing => UpdateArtwork(existing, incoming));

                        // _logger.LogWarning(
                        //     "Error processing plex movie at {Key}: {Error}",
                        //     incoming.Key,
                        //     error.Value);
                    }

                    var incomingMovieIds = movies.Map(s => s.ItemId).ToList();
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
