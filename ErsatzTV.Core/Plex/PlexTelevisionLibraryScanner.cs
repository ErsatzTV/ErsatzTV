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

            return await entries.Match<Task<Either<BaseError, Unit>>>(
                async showEntries =>
                {
                    foreach (PlexShow incoming in showEntries)
                    {
                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexShow> maybeShow = await _televisionRepository
                            .GetOrAddPlexShow(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeShow.Match(
                            async show =>
                            {
                                // await _televisionRepository.Update(show);
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

                    return Unit.Default;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        plexMediaSourceLibrary.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
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

            return await entries.Match<Task<Either<BaseError, Unit>>>(
                async seasonEntries =>
                {
                    foreach (PlexSeason incoming in seasonEntries)
                    {
                        incoming.ShowId = show.Id;

                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexSeason> maybeSeason = await _televisionRepository
                            .GetOrAddPlexSeason(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeSeason.Match(
                            async season =>
                            {
                                // await _televisionRepository.Update(season);
                                await ScanEpisodes(plexMediaSourceLibrary, season, connection, token);
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

                    return Unit.Default;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        plexMediaSourceLibrary.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
        }

        private Task<Either<BaseError, PlexSeason>> UpdateMetadata(PlexSeason existing, PlexSeason incoming) =>
            Right<BaseError, PlexSeason>(existing).AsTask();

        private Task<Either<BaseError, PlexSeason>> UpdateArtwork(PlexSeason existing, PlexSeason incoming) =>
            Right<BaseError, PlexSeason>(existing).AsTask();
        
        private async Task<Either<BaseError, Unit>> ScanEpisodes(
            PlexLibrary plexMediaSourceLibrary,
            PlexSeason season,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            Either<BaseError, List<PlexEpisode>> entries = await _plexServerApiClient.GetSeasonEpisodes(
                plexMediaSourceLibrary,
                season,
                connection,
                token);

            return await entries.Match<Task<Either<BaseError, Unit>>>(
                async episodeEntries =>
                {
                    foreach (PlexEpisode incoming in episodeEntries)
                    {
                        incoming.SeasonId = season.Id;

                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexEpisode> maybeEpisode = await _televisionRepository
                            .GetOrAddPlexEpisode(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeEpisode.Match(
                            async episode =>
                            {
                                // await _televisionRepository.Update(episode);
                            },
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing plex episode at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
                    }

                    return Unit.Default;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        plexMediaSourceLibrary.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
        }
        
        private Task<Either<BaseError, PlexEpisode>> UpdateStatistics(PlexEpisode existing, PlexEpisode incoming) =>
            Right<BaseError, PlexEpisode>(existing).AsTask();
        
        private Task<Either<BaseError, PlexEpisode>> UpdateMetadata(PlexEpisode existing, PlexEpisode incoming) =>
            Right<BaseError, PlexEpisode>(existing).AsTask();

        private Task<Either<BaseError, PlexEpisode>> UpdateArtwork(PlexEpisode existing, PlexEpisode incoming) =>
            Right<BaseError, PlexEpisode>(existing).AsTask();
    }
}
