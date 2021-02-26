using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex
{
    public class PlexMovieLibraryScanner : IPlexMovieLibraryScanner
    {
        private readonly ILogger<PlexMovieLibraryScanner> _logger;
        private readonly IMovieRepository _movieRepository;
        private readonly IPlexServerApiClient _plexServerApiClient;

        public PlexMovieLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            IMovieRepository movieRepository,
            ILogger<PlexMovieLibraryScanner> logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _movieRepository = movieRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary)
        {
            Either<BaseError, List<PlexMovie>> entries = await _plexServerApiClient.GetLibraryContents(
                plexMediaSourceLibrary,
                connection,
                token);

            await entries.Match(
                async movieEntries =>
                {
                    foreach (PlexMovie entry in movieEntries)
                    {
                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexMovie> maybeMovie = await _movieRepository
                            .GetOrAdd(plexMediaSourceLibrary, entry)
                            .BindT(UpdateIfNeeded);

                        maybeMovie.IfLeft(
                            error => _logger.LogWarning(
                                "Error processing plex movie at {Key}: {Error}",
                                entry.Key,
                                error.Value));
                    }
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        plexMediaSourceLibrary.Name,
                        error.Value);

                    return Task.CompletedTask;
                });

            // need plex media item model that can be used to lookup by unique id (metadata key?)

            return Unit.Default;
        }

        private async Task<Either<BaseError, PlexMovie>> UpdateIfNeeded(PlexMovie plexMovie) =>
            // .BindT(movie => UpdateStatistics(movie, ffprobePath).MapT(_ => movie))
            //     .BindT(UpdateMetadata)
            //     .BindT(UpdatePoster);
            plexMovie;
    }
}
