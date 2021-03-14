using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Plex
{
    public class PlexTelevisionLibraryScanner : IPlexTelevisionLibraryScanner
    {
        private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
        private readonly IPlexServerApiClient _plexServerApiClient;
        private readonly ITelevisionRepository _televisionRepository;

        public PlexTelevisionLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            ITelevisionRepository televisionRepository,
            ILogger<PlexTelevisionLibraryScanner> logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _televisionRepository = televisionRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary)
        {
            Either<BaseError, List<PlexShow>> entries = await _plexServerApiClient.GetShowLibraryContents(
                plexMediaSourceLibrary,
                connection,
                token);

            await entries.Match(
                async showEntries =>
                {
                    foreach (PlexShow incoming in showEntries)
                    {
                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexShow> maybeShow = await _televisionRepository
                            .GetOrAdd(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeShow.Match(
                            async show =>
                            {
                                await _televisionRepository.Update(show);
                                await ScanSeasons(plexMediaSourceLibrary, show, connection, token);
                            },
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing plex show at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
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

            return Unit.Default;
        }

        private Task<Either<BaseError, PlexShow>> UpdateMetadata(PlexShow existing, PlexShow incoming) =>
            Right<BaseError, PlexShow>(existing).AsTask();

        private Task<Either<BaseError, PlexShow>> UpdateArtwork(PlexShow existing, PlexShow incoming) =>
            Right<BaseError, PlexShow>(existing).AsTask();

        private async Task<Either<BaseError, Unit>> ScanSeasons(
            PlexLibrary plexMediaSourceLibrary,
            PlexShow show,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            Either<BaseError, List<PlexSeason>> entries = await _plexServerApiClient.GetShowSeasons(
                plexMediaSourceLibrary,
                show,
                connection,
                token);

            await entries.Match(
                async seasonEntries =>
                {
                    foreach (PlexSeason incoming in seasonEntries)
                    {
                        incoming.ShowId = show.Id;

                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexSeason> maybeSeason = await _televisionRepository
                            .GetOrAdd(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeSeason.Match(
                            async season =>
                            {
                                await _televisionRepository.Update(season);
                                // await ScanSeasons(plexMediaSourceLibrary, season, connection, token);
                            },
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing plex show at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
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

            return Unit.Default;
        }

        private Task<Either<BaseError, PlexSeason>> UpdateMetadata(PlexSeason existing, PlexSeason incoming) =>
            Right<BaseError, PlexSeason>(existing).AsTask();

        private Task<Either<BaseError, PlexSeason>> UpdateArtwork(PlexSeason existing, PlexSeason incoming) =>
            Right<BaseError, PlexSeason>(existing).AsTask();
    }
}
