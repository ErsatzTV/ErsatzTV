using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Plex
{
    public class PlexTelevisionLibraryScanner : PlexLibraryScanner, IPlexTelevisionLibraryScanner
    {
        private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly IPlexServerApiClient _plexServerApiClient;
        private readonly ITelevisionRepository _televisionRepository;

        public PlexTelevisionLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            ITelevisionRepository televisionRepository,
            IMediaItemRepository mediaItemRepository,
            ILogger<PlexTelevisionLibraryScanner> logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _televisionRepository = televisionRepository;
            _mediaItemRepository = mediaItemRepository;
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

                    // TODO: delete removed shows

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

        private Task<Either<BaseError, PlexShow>> UpdateMetadata(PlexShow existing, PlexShow incoming)
        {
            ShowMetadata existingMetadata = existing.ShowMetadata.Head();
            ShowMetadata incomingMetadata = incoming.ShowMetadata.Head();

            // TODO: this probably doesn't work
            // plex doesn't seem to update genres returned by the main library call
            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                foreach (Genre genre in existingMetadata.Genres
                    .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Remove(genre);
                    _mediaItemRepository.RemoveGenre(genre);
                }

                foreach (Genre genre in incomingMetadata.Genres
                    .Filter(g => existingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Add(genre);
                    _televisionRepository.AddGenre(existingMetadata, genre);
                }
            }

            return Right<BaseError, PlexShow>(existing).AsTask();
        }

        private Task<Either<BaseError, PlexShow>> UpdateArtwork(PlexShow existing, PlexShow incoming)
        {
            ShowMetadata existingMetadata = existing.ShowMetadata.Head();
            ShowMetadata incomingMetadata = incoming.ShowMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
            }

            return Right<BaseError, PlexShow>(existing).AsTask();
        }

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

                    // TODO: delete removed seasons

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

        private Task<Either<BaseError, PlexSeason>> UpdateArtwork(PlexSeason existing, PlexSeason incoming)
        {
            SeasonMetadata existingMetadata = existing.SeasonMetadata.Head();
            SeasonMetadata incomingMetadata = incoming.SeasonMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
            }

            return Right<BaseError, PlexSeason>(existing).AsTask();
        }

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
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
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

                    // TODO: delete removed episodes

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

        private async Task<Either<BaseError, PlexEpisode>> UpdateStatistics(
            PlexEpisode existing,
            PlexEpisode incoming,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            MediaVersion existingVersion = existing.MediaVersions.Head();
            MediaVersion incomingVersion = incoming.MediaVersions.Head();

            if (incomingVersion.DateUpdated > existingVersion.DateUpdated ||
                string.IsNullOrWhiteSpace(existingVersion.SampleAspectRatio))
            {
                Either<BaseError, MediaVersion> maybeStatistics =
                    await _plexServerApiClient.GetStatistics(incoming.Key.Split("/").Last(), connection, token);

                await maybeStatistics.Match(
                    async mediaVersion =>
                    {
                        existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio ?? "1:1";
                        existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                        existingVersion.DateUpdated = incomingVersion.DateUpdated;

                        await _mediaItemRepository.UpdateStatistics(existingVersion);
                    },
                    _ => Task.CompletedTask);
            }

            return Right<BaseError, PlexEpisode>(existing);
        }

        private Task<Either<BaseError, PlexEpisode>> UpdateArtwork(PlexEpisode existing, PlexEpisode incoming)
        {
            EpisodeMetadata existingMetadata = existing.EpisodeMetadata.Head();
            EpisodeMetadata incomingMetadata = incoming.EpisodeMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Thumbnail);
            }

            return Right<BaseError, PlexEpisode>(existing).AsTask();
        }
    }
}
