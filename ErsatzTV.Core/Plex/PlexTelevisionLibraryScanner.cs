using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Plex
{
    public class PlexTelevisionLibraryScanner : PlexLibraryScanner, IPlexTelevisionLibraryScanner
    {
        private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
        private readonly IMediator _mediator;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IPlexServerApiClient _plexServerApiClient;
        private readonly ISearchIndex _searchIndex;
        private readonly ITelevisionRepository _televisionRepository;

        public PlexTelevisionLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            ITelevisionRepository televisionRepository,
            IMetadataRepository metadataRepository,
            ISearchIndex searchIndex,
            IMediator mediator,
            ILogger<PlexTelevisionLibraryScanner> logger)
            : base(metadataRepository, logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _televisionRepository = televisionRepository;
            _metadataRepository = metadataRepository;
            _searchIndex = searchIndex;
            _mediator = mediator;
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
                        decimal percentCompletion = (decimal) showEntries.IndexOf(incoming) / showEntries.Count;
                        await _mediator.Publish(new LibraryScanProgress(plexMediaSourceLibrary.Id, percentCompletion));

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, MediaItemScanResult<PlexShow>> maybeShow = await _televisionRepository
                            .GetOrAddPlexShow(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeShow.Match(
                            async result =>
                            {
                                if (result.IsAdded)
                                {
                                    await _searchIndex.AddItems(new List<MediaItem> { result.Item });
                                }
                                else if (result.IsUpdated)
                                {
                                    await _searchIndex.UpdateItems(new List<MediaItem> { result.Item });
                                }

                                await ScanSeasons(plexMediaSourceLibrary, result.Item, connection, token);
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

                    var showKeys = showEntries.Map(s => s.Key).ToList();
                    List<int> ids =
                        await _televisionRepository.RemoveMissingPlexShows(plexMediaSourceLibrary, showKeys);
                    await _searchIndex.RemoveItems(ids);

                    await _mediator.Publish(new LibraryScanProgress(plexMediaSourceLibrary.Id, 0));

                    _searchIndex.Commit();
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

        private async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> UpdateMetadata(
            MediaItemScanResult<PlexShow> result,
            PlexShow incoming)
        {
            PlexShow existing = result.Item;
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
                    if (await _metadataRepository.RemoveGenre(genre))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Genre genre in incomingMetadata.Genres
                    .Filter(g => existingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Add(genre);
                    if (await _televisionRepository.AddGenre(existingMetadata, genre))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Studio studio in existingMetadata.Studios
                    .Filter(s => incomingMetadata.Studios.All(s2 => s2.Name != s.Name))
                    .ToList())
                {
                    existingMetadata.Studios.Remove(studio);
                    if (await _metadataRepository.RemoveStudio(studio))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Studio studio in incomingMetadata.Studios
                    .Filter(s => existingMetadata.Studios.All(s2 => s2.Name != s.Name))
                    .ToList())
                {
                    existingMetadata.Studios.Add(studio);
                    if (await _televisionRepository.AddStudio(existingMetadata, studio))
                    {
                        result.IsUpdated = true;
                    }
                }

                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }

            return result;
        }

        private async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> UpdateArtwork(
            MediaItemScanResult<PlexShow> result,
            PlexShow incoming)
        {
            PlexShow existing = result.Item;
            ShowMetadata existingMetadata = existing.ShowMetadata.Head();
            ShowMetadata incomingMetadata = incoming.ShowMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }

            return result;
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

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexSeason> maybeSeason = await _televisionRepository
                            .GetOrAddPlexSeason(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeSeason.Match(
                            async season => await ScanEpisodes(plexMediaSourceLibrary, season, connection, token),
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing plex show at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
                    }

                    var seasonKeys = seasonEntries.Map(s => s.Key).ToList();
                    await _televisionRepository.RemoveMissingPlexSeasons(show.Key, seasonKeys);

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

        private async Task<Either<BaseError, PlexSeason>> UpdateArtwork(PlexSeason existing, PlexSeason incoming)
        {
            SeasonMetadata existingMetadata = existing.SeasonMetadata.Head();
            SeasonMetadata incomingMetadata = incoming.SeasonMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }

            return existing;
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

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexEpisode> maybeEpisode = await _televisionRepository
                            .GetOrAddPlexEpisode(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        maybeEpisode.IfLeft(
                            error => _logger.LogWarning(
                                "Error processing plex episode at {Key}: {Error}",
                                incoming.Key,
                                error.Value));
                    }

                    var episodeKeys = episodeEntries.Map(s => s.Key).ToList();
                    await _televisionRepository.RemoveMissingPlexEpisodes(season.Key, episodeKeys);

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

            if (incomingVersion.DateUpdated > existingVersion.DateUpdated || !existingVersion.Streams.Any())
            {
                Either<BaseError, MediaVersion> maybeStatistics =
                    await _plexServerApiClient.GetStatistics(incoming.Key.Split("/").Last(), connection, token);

                await maybeStatistics.Match(
                    async mediaVersion =>
                    {
                        existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio;
                        existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                        existingVersion.DateUpdated = mediaVersion.DateUpdated;

                        await _metadataRepository.UpdatePlexStatistics(existingVersion.Id, mediaVersion);
                    },
                    _ => Task.CompletedTask);
            }

            return Right<BaseError, PlexEpisode>(existing);
        }

        private async Task<Either<BaseError, PlexEpisode>> UpdateArtwork(PlexEpisode existing, PlexEpisode incoming)
        {
            EpisodeMetadata existingMetadata = existing.EpisodeMetadata.Head();
            EpisodeMetadata incomingMetadata = incoming.EpisodeMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Thumbnail);
            }

            return existing;
        }
    }
}
