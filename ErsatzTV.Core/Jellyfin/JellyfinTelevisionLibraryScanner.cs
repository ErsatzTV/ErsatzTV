using System;
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
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Jellyfin
{
    public class JellyfinTelevisionLibraryScanner : IJellyfinTelevisionLibraryScanner
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger<JellyfinTelevisionLibraryScanner> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMediator _mediator;
        private readonly IJellyfinPathReplacementService _pathReplacementService;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;
        private readonly IJellyfinTelevisionRepository _televisionRepository;

        public JellyfinTelevisionLibraryScanner(
            IJellyfinApiClient jellyfinApiClient,
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinTelevisionRepository televisionRepository,
            ISearchIndex searchIndex,
            ISearchRepository searchRepository,
            IJellyfinPathReplacementService pathReplacementService,
            ILocalFileSystem localFileSystem,
            ILocalStatisticsProvider localStatisticsProvider,
            IMediator mediator,
            ILogger<JellyfinTelevisionLibraryScanner> logger)
        {
            _jellyfinApiClient = jellyfinApiClient;
            _mediaSourceRepository = mediaSourceRepository;
            _televisionRepository = televisionRepository;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
            _pathReplacementService = pathReplacementService;
            _localFileSystem = localFileSystem;
            _localStatisticsProvider = localStatisticsProvider;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            string address,
            string apiKey,
            JellyfinLibrary library,
            string ffprobePath)
        {
            List<JellyfinItemEtag> existingShows = await _televisionRepository.GetExistingShows(library);

            // TODO: maybe get quick list of item ids and etags from api to compare first
            // TODO: paging?

            List<JellyfinPathReplacement> pathReplacements = await _mediaSourceRepository
                .GetJellyfinPathReplacements(library.MediaSourceId);

            Either<BaseError, List<JellyfinShow>> maybeShows = await _jellyfinApiClient.GetShowLibraryItems(
                address,
                apiKey,
                library.MediaSourceId,
                library.ItemId);

            await maybeShows.Match(
                async shows =>
                {
                    await ProcessShows(address, apiKey, library, ffprobePath, pathReplacements, existingShows, shows);

                    var incomingShowIds = shows.Map(s => s.ItemId).ToList();
                    var showIds = existingShows
                        .Filter(i => !incomingShowIds.Contains(i.ItemId))
                        .Map(m => m.ItemId)
                        .ToList();
                    List<int> missingShowIds = await _televisionRepository.RemoveMissingShows(library, showIds);
                    await _searchIndex.RemoveItems(missingShowIds);

                    await _televisionRepository.DeleteEmptySeasons(library);
                    List<int> emptyShowIds = await _televisionRepository.DeleteEmptyShows(library);
                    await _searchIndex.RemoveItems(emptyShowIds);

                    await _mediator.Publish(new LibraryScanProgress(library.Id, 0));
                    _searchIndex.Commit();
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing jellyfin library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Task.CompletedTask;
                });

            return Unit.Default;
        }

        private async Task ProcessShows(
            string address,
            string apiKey,
            JellyfinLibrary library,
            string ffprobePath,
            List<JellyfinPathReplacement> pathReplacements,
            List<JellyfinItemEtag> existingShows,
            List<JellyfinShow> shows)
        {
            foreach (JellyfinShow incoming in shows.OrderBy(s => s.ShowMetadata.Head().Title))
            {
                decimal percentCompletion = (decimal) shows.IndexOf(incoming) / shows.Count;
                await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                var changed = false;

                Option<JellyfinItemEtag> maybeExisting = existingShows.Find(ie => ie.ItemId == incoming.ItemId);
                await maybeExisting.Match(
                    async existing =>
                    {
                        if (existing.Etag == incoming.Etag)
                        {
                            return;
                        }

                        _logger.LogDebug(
                            "UPDATE: Etag has changed for show {Show}",
                            incoming.ShowMetadata.Head().Title);

                        changed = true;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        Option<JellyfinShow> updated = await _televisionRepository.Update(incoming);
                        if (updated.IsSome)
                        {
                            await _searchIndex.UpdateItems(
                                _searchRepository,
                                new List<MediaItem> { updated.ValueUnsafe() });
                        }
                    },
                    async () =>
                    {
                        changed = true;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        // _logger.LogDebug("INSERT: Item id is new for show {Show}", incoming.ShowMetadata.Head().Title);

                        if (await _televisionRepository.AddShow(incoming))
                        {
                            await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                        }
                    });

                if (changed)
                {
                    List<JellyfinItemEtag> existingSeasons =
                        await _televisionRepository.GetExistingSeasons(library, incoming.ItemId);

                    Either<BaseError, List<JellyfinSeason>> maybeSeasons =
                        await _jellyfinApiClient.GetSeasonLibraryItems(
                            address,
                            apiKey,
                            library.MediaSourceId,
                            incoming.ItemId);

                    await maybeSeasons.Match(
                        async seasons =>
                        {
                            await ProcessSeasons(
                                address,
                                apiKey,
                                library,
                                ffprobePath,
                                pathReplacements,
                                incoming,
                                existingSeasons,
                                seasons);

                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { incoming });

                            var incomingSeasonIds = seasons.Map(s => s.ItemId).ToList();
                            var seasonIds = existingSeasons
                                .Filter(i => !incomingSeasonIds.Contains(i.ItemId))
                                .Map(m => m.ItemId)
                                .ToList();
                            await _televisionRepository.RemoveMissingSeasons(library, seasonIds);
                        },
                        error =>
                        {
                            _logger.LogWarning(
                                "Error synchronizing jellyfin library {Path}: {Error}",
                                library.Name,
                                error.Value);

                            return Task.CompletedTask;
                        });
                }
            }
        }

        private async Task ProcessSeasons(
            string address,
            string apiKey,
            JellyfinLibrary library,
            string ffprobePath,
            List<JellyfinPathReplacement> pathReplacements,
            JellyfinShow show,
            List<JellyfinItemEtag> existingSeasons,
            List<JellyfinSeason> seasons)
        {
            foreach (JellyfinSeason incoming in seasons)
            {
                var changed = false;

                Option<JellyfinItemEtag> maybeExisting = existingSeasons.Find(ie => ie.ItemId == incoming.ItemId);
                await maybeExisting.Match(
                    async existing =>
                    {
                        if (existing.Etag == incoming.Etag)
                        {
                            return;
                        }

                        _logger.LogDebug(
                            "UPDATE: Etag has changed for show {Show} season {Season}",
                            show.ShowMetadata.Head().Title,
                            incoming.SeasonMetadata.Head().Title);

                        changed = true;
                        incoming.ShowId = show.Id;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        await _televisionRepository.Update(incoming);
                    },
                    async () =>
                    {
                        changed = true;
                        incoming.ShowId = show.Id;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        _logger.LogDebug(
                            "INSERT: Item id is new for show {Show} season {Season}",
                            show.ShowMetadata.Head().Title,
                            incoming.SeasonMetadata.Head().Title);

                        await _televisionRepository.AddSeason(incoming);
                    });

                if (changed)
                {
                    List<JellyfinItemEtag> existingEpisodes =
                        await _televisionRepository.GetExistingEpisodes(library, incoming.ItemId);

                    Either<BaseError, List<JellyfinEpisode>> maybeEpisodes =
                        await _jellyfinApiClient.GetEpisodeLibraryItems(
                            address,
                            apiKey,
                            library.MediaSourceId,
                            incoming.ItemId);

                    await maybeEpisodes.Match(
                        async episodes =>
                        {
                            var validEpisodes = new List<JellyfinEpisode>();
                            foreach (JellyfinEpisode episode in episodes)
                            {
                                string localPath = _pathReplacementService.GetReplacementJellyfinPath(
                                    pathReplacements,
                                    episode.MediaVersions.Head().MediaFiles.Head().Path,
                                    false);

                                if (!_localFileSystem.FileExists(localPath))
                                {
                                    _logger.LogWarning(
                                        "Skipping jellyfin episode that does not exist at {Path}",
                                        localPath);
                                }
                                else
                                {
                                    validEpisodes.Add(episode);
                                }
                            }

                            await ProcessEpisodes(
                                show.ShowMetadata.Head().Title,
                                incoming.SeasonMetadata.Head().Title,
                                library,
                                ffprobePath,
                                pathReplacements,
                                incoming,
                                existingEpisodes,
                                validEpisodes);

                            var incomingEpisodeIds = episodes.Map(s => s.ItemId).ToList();
                            var episodeIds = existingEpisodes
                                .Filter(i => !incomingEpisodeIds.Contains(i.ItemId))
                                .Map(m => m.ItemId)
                                .ToList();
                            await _televisionRepository.RemoveMissingEpisodes(library, episodeIds);
                        },
                        error =>
                        {
                            _logger.LogWarning(
                                "Error synchronizing jellyfin library {Path}: {Error}",
                                library.Name,
                                error.Value);

                            return Task.CompletedTask;
                        });
                }
            }
        }

        private async Task ProcessEpisodes(
            string showName,
            string seasonName,
            JellyfinLibrary library,
            string ffprobePath,
            List<JellyfinPathReplacement> pathReplacements,
            JellyfinSeason season,
            List<JellyfinItemEtag> existingEpisodes,
            List<JellyfinEpisode> episodes)
        {
            foreach (JellyfinEpisode incoming in episodes)
            {
                var updateStatistics = false;

                Option<JellyfinItemEtag> maybeExisting = existingEpisodes.Find(ie => ie.ItemId == incoming.ItemId);
                await maybeExisting.Match(
                    async existing =>
                    {
                        try
                        {
                            if (existing.Etag == incoming.Etag)
                            {
                                return;
                            }

                            _logger.LogDebug(
                                "UPDATE: Etag has changed for show {Show} season {Season} episode {Episode}",
                                showName,
                                seasonName,
                                "EPISODE");

                            updateStatistics = true;
                            incoming.SeasonId = season.Id;
                            incoming.LibraryPathId = library.Paths.Head().Id;

                            await _televisionRepository.Update(incoming);
                        }
                        catch (Exception ex)
                        {
                            updateStatistics = false;
                            _logger.LogError(
                                ex,
                                "Error updating episode {Path}",
                                incoming.MediaVersions.Head().MediaFiles.Head().Path);
                        }
                    },
                    async () =>
                    {
                        try
                        {
                            updateStatistics = true;
                            incoming.SeasonId = season.Id;
                            incoming.LibraryPathId = library.Paths.Head().Id;

                            _logger.LogDebug(
                                "INSERT: Item id is new for show {Show} season {Season} episode {Episode}",
                                showName,
                                seasonName,
                                "EPISODE");

                            await _televisionRepository.AddEpisode(incoming);
                        }
                        catch (Exception ex)
                        {
                            updateStatistics = false;
                            _logger.LogError(
                                ex,
                                "Error adding episode {Path}",
                                incoming.MediaVersions.Head().MediaFiles.Head().Path);
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
            }
        }
    }
}
