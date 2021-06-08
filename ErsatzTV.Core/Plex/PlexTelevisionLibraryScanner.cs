using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Plex
{
    public class PlexTelevisionLibraryScanner : PlexLibraryScanner, IPlexTelevisionLibraryScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMediator _mediator;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IPlexPathReplacementService _plexPathReplacementService;
        private readonly IPlexServerApiClient _plexServerApiClient;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public PlexTelevisionLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            ITelevisionRepository televisionRepository,
            IMetadataRepository metadataRepository,
            ISearchIndex searchIndex,
            ISearchRepository searchRepository,
            IMediator mediator,
            IMediaSourceRepository mediaSourceRepository,
            IPlexPathReplacementService plexPathReplacementService,
            ILocalFileSystem localFileSystem,
            ILogger<PlexTelevisionLibraryScanner> logger)
            : base(metadataRepository, logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _televisionRepository = televisionRepository;
            _metadataRepository = metadataRepository;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
            _mediator = mediator;
            _mediaSourceRepository = mediaSourceRepository;
            _plexPathReplacementService = plexPathReplacementService;
            _localFileSystem = localFileSystem;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary library)
        {
            List<PlexPathReplacement> pathReplacements = await _mediaSourceRepository
                .GetPlexPathReplacements(library.MediaSourceId);

            Either<BaseError, List<PlexShow>> entries = await _plexServerApiClient.GetShowLibraryContents(
                library,
                connection,
                token);

            return await entries.Match<Task<Either<BaseError, Unit>>>(
                async showEntries =>
                {
                    foreach (PlexShow incoming in showEntries)
                    {
                        decimal percentCompletion = (decimal) showEntries.IndexOf(incoming) / showEntries.Count;
                        await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, MediaItemScanResult<PlexShow>> maybeShow = await _televisionRepository
                            .GetOrAddPlexShow(library, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming, library, connection, token))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeShow.Match(
                            async result =>
                            {
                                await ScanSeasons(library, pathReplacements, result.Item, connection, token);

                                if (result.IsAdded)
                                {
                                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                                }
                                else if (result.IsUpdated)
                                {
                                    await _searchIndex.UpdateItems(
                                        _searchRepository,
                                        new List<MediaItem> { result.Item });
                                }
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
                        await _televisionRepository.RemoveMissingPlexShows(library, showKeys);
                    await _searchIndex.RemoveItems(ids);

                    await _mediator.Publish(new LibraryScanProgress(library.Id, 0));

                    _searchIndex.Commit();
                    return Unit.Default;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
        }

        private async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> UpdateMetadata(
            MediaItemScanResult<PlexShow> result,
            PlexShow incoming,
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            PlexShow existing = result.Item;
            ShowMetadata existingMetadata = existing.ShowMetadata.Head();

            if (result.IsAdded || incoming.ShowMetadata.Head().DateUpdated > existingMetadata.DateUpdated)
            {
                Either<BaseError, ShowMetadata> maybeMetadata =
                    await _plexServerApiClient.GetShowMetadata(
                        library,
                        incoming.Key.Replace("/children", string.Empty).Split("/").Last(),
                        connection,
                        token);

                await maybeMetadata.Match(
                    async fullMetadata =>
                    {
                        if (existingMetadata.MetadataKind != MetadataKind.External)
                        {
                            existingMetadata.MetadataKind = MetadataKind.External;
                            await _metadataRepository.MarkAsExternal(existingMetadata);
                        }

                        if (existingMetadata.ContentRating != fullMetadata.ContentRating)
                        {
                            existingMetadata.ContentRating = fullMetadata.ContentRating;
                            await _metadataRepository.SetContentRating(existingMetadata, fullMetadata.ContentRating);
                            result.IsUpdated = true;
                        }

                        foreach (Genre genre in existingMetadata.Genres
                            .Filter(g => fullMetadata.Genres.All(g2 => g2.Name != g.Name))
                            .ToList())
                        {
                            existingMetadata.Genres.Remove(genre);
                            if (await _metadataRepository.RemoveGenre(genre))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (Genre genre in fullMetadata.Genres
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
                            .Filter(s => fullMetadata.Studios.All(s2 => s2.Name != s.Name))
                            .ToList())
                        {
                            existingMetadata.Studios.Remove(studio);
                            if (await _metadataRepository.RemoveStudio(studio))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (Studio studio in fullMetadata.Studios
                            .Filter(s => existingMetadata.Studios.All(s2 => s2.Name != s.Name))
                            .ToList())
                        {
                            existingMetadata.Studios.Add(studio);
                            if (await _televisionRepository.AddStudio(existingMetadata, studio))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (Actor actor in existingMetadata.Actors
                            .Filter(
                                a => fullMetadata.Actors.All(
                                    a2 => a2.Name != a.Name || a.Artwork == null && a2.Artwork != null))
                            .ToList())
                        {
                            existingMetadata.Actors.Remove(actor);
                            if (await _metadataRepository.RemoveActor(actor))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (Actor actor in fullMetadata.Actors
                            .Filter(a => existingMetadata.Actors.All(a2 => a2.Name != a.Name))
                            .ToList())
                        {
                            existingMetadata.Actors.Add(actor);
                            if (await _televisionRepository.AddActor(existingMetadata, actor))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (MetadataGuid guid in existingMetadata.Guids
                            .Filter(g => fullMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                            .ToList())
                        {
                            existingMetadata.Guids.Remove(guid);
                            if (await _metadataRepository.RemoveGuid(guid))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        foreach (MetadataGuid guid in fullMetadata.Guids
                            .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                            .ToList())
                        {
                            existingMetadata.Guids.Add(guid);
                            if (await _metadataRepository.AddGuid(existingMetadata, guid))
                            {
                                result.IsUpdated = true;
                            }
                        }

                        if (result.IsUpdated)
                        {
                            await _metadataRepository.MarkAsUpdated(existingMetadata, fullMetadata.DateUpdated);
                        }
                    },
                    _ => Task.CompletedTask);
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
            PlexLibrary library,
            List<PlexPathReplacement> pathReplacements,
            PlexShow show,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            Either<BaseError, List<PlexSeason>> entries = await _plexServerApiClient.GetShowSeasons(
                library,
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
                            .GetOrAddPlexSeason(library, incoming)
                            .BindT(existing => UpdateMetadataAndArtwork(existing, incoming));

                        await maybeSeason.Match(
                            async season => await ScanEpisodes(library, pathReplacements, season, connection, token),
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
                        library.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
        }

        private async Task<Either<BaseError, PlexSeason>> UpdateMetadataAndArtwork(
            PlexSeason existing,
            PlexSeason incoming)
        {
            SeasonMetadata existingMetadata = existing.SeasonMetadata.Head();
            SeasonMetadata incomingMetadata = incoming.SeasonMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                foreach (MetadataGuid guid in existingMetadata.Guids
                    .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                    .ToList())
                {
                    existingMetadata.Guids.Remove(guid);
                    await _metadataRepository.RemoveGuid(guid);
                }

                foreach (MetadataGuid guid in incomingMetadata.Guids
                    .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                    .ToList())
                {
                    existingMetadata.Guids.Add(guid);
                    await _metadataRepository.AddGuid(existingMetadata, guid);
                }

                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }

            return existing;
        }

        private async Task<Either<BaseError, Unit>> ScanEpisodes(
            PlexLibrary library,
            List<PlexPathReplacement> pathReplacements,
            PlexSeason season,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            Either<BaseError, List<PlexEpisode>> entries = await _plexServerApiClient.GetSeasonEpisodes(
                library,
                season,
                connection,
                token);

            return await entries.Match<Task<Either<BaseError, Unit>>>(
                async episodeEntries =>
                {
                    var validEpisodes = new List<PlexEpisode>();
                    foreach (PlexEpisode episode in episodeEntries)
                    {
                        string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                            pathReplacements,
                            episode.MediaVersions.Head().MediaFiles.Head().Path,
                            false);

                        if (!_localFileSystem.FileExists(localPath))
                        {
                            _logger.LogWarning(
                                "Skipping plex episode that does not exist at {Path}",
                                localPath);
                        }
                        else
                        {
                            validEpisodes.Add(episode);
                        }
                    }

                    foreach (PlexEpisode incoming in validEpisodes)
                    {
                        incoming.SeasonId = season.Id;

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexEpisode> maybeEpisode = await _televisionRepository
                            .GetOrAddPlexEpisode(library, incoming)
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(
                                existing => UpdateStatistics(
                                    existing,
                                    incoming,
                                    library,
                                    connection,
                                    token))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeEpisode.Match(
                            async episode =>
                            {
                                await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { episode });
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

                    var episodeKeys = validEpisodes.Map(s => s.Key).ToList();
                    List<int> ids = await _televisionRepository.RemoveMissingPlexEpisodes(season.Key, episodeKeys);
                    await _searchIndex.RemoveItems(ids);
                    _searchIndex.Commit();

                    return Unit.Default;
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Left<BaseError, Unit>(error).AsTask();
                });
        }

        private async Task<Either<BaseError, PlexEpisode>> UpdateMetadata(PlexEpisode existing, PlexEpisode incoming)
        {
            var toUpdate = existing.EpisodeMetadata
                .Where(em => incoming.EpisodeMetadata.Any(em2 => em2.EpisodeNumber == em.EpisodeNumber))
                .ToList();
            var toRemove = existing.EpisodeMetadata.Except(toUpdate).ToList();
            var toAdd = incoming.EpisodeMetadata
                .Where(em => existing.EpisodeMetadata.All(em2 => em2.EpisodeNumber != em.EpisodeNumber))
                .ToList();

            foreach (EpisodeMetadata metadata in toRemove)
            {
                await _televisionRepository.RemoveMetadata(existing, metadata);
            }

            foreach (EpisodeMetadata metadata in toAdd)
            {
                metadata.EpisodeId = existing.Id;
                metadata.Episode = existing;
                existing.EpisodeMetadata.Add(metadata);

                await _metadataRepository.Add(metadata);
            }

            // TODO: update existing metadata

            return existing;
        }

        private async Task<Either<BaseError, PlexEpisode>> UpdateStatistics(
            PlexEpisode existing,
            PlexEpisode incoming,
            PlexLibrary library,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            MediaVersion existingVersion = existing.MediaVersions.Head();
            MediaVersion incomingVersion = incoming.MediaVersions.Head();

            if (incomingVersion.DateUpdated > existingVersion.DateUpdated || !existingVersion.Streams.Any())
            {
                Either<BaseError, Tuple<EpisodeMetadata, MediaVersion>> maybeStatistics =
                    await _plexServerApiClient.GetEpisodeMetadataAndStatistics(
                        library,
                        incoming.Key.Split("/").Last(),
                        connection,
                        token);

                await maybeStatistics.Match(
                    async tuple =>
                    {
                        (EpisodeMetadata incomingMetadata, MediaVersion mediaVersion) = tuple;

                        Option<EpisodeMetadata> maybeExisting = existing.EpisodeMetadata
                            .Find(em => em.EpisodeNumber == incomingMetadata.EpisodeNumber);
                        foreach (EpisodeMetadata existingMetadata in maybeExisting)
                        {
                            foreach (MetadataGuid guid in existingMetadata.Guids
                                .Filter(g => incomingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                                .ToList())
                            {
                                existingMetadata.Guids.Remove(guid);
                                await _metadataRepository.RemoveGuid(guid);
                            }

                            foreach (MetadataGuid guid in incomingMetadata.Guids
                                .Filter(g => existingMetadata.Guids.All(g2 => g2.Guid != g.Guid))
                                .ToList())
                            {
                                existingMetadata.Guids.Add(guid);
                                await _metadataRepository.AddGuid(existingMetadata, guid);
                            }
                        }

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
            foreach (EpisodeMetadata incomingMetadata in incoming.EpisodeMetadata)
            {
                Option<EpisodeMetadata> maybeExistingMetadata = existing.EpisodeMetadata
                    .Find(em => em.EpisodeNumber == incomingMetadata.EpisodeNumber);
                if (maybeExistingMetadata.IsSome)
                {
                    EpisodeMetadata existingMetadata = maybeExistingMetadata.ValueUnsafe();
                    if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
                    {
                        await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Thumbnail);
                        await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
                    }
                }
            }

            return existing;
        }
    }
}
