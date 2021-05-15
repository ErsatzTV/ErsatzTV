using System.Collections.Generic;
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
    public class JellyfinTelevisionLibraryScanner : IJellyfinTelevisionLibraryScanner
    {
        private readonly IJellyfinApiClient _jellyfinApiClient;
        private readonly ILogger<JellyfinTelevisionLibraryScanner> _logger;
        private readonly IMediaSourceRepository _mediaSourceRepository;
        private readonly IMediator _mediator;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;
        private readonly IJellyfinTelevisionRepository _televisionRepository;

        public JellyfinTelevisionLibraryScanner(
            IJellyfinApiClient jellyfinApiClient,
            IMediaSourceRepository mediaSourceRepository,
            IJellyfinTelevisionRepository televisionRepository,
            ISearchIndex searchIndex,
            ISearchRepository searchRepository,
            IMediator mediator,
            ILogger<JellyfinTelevisionLibraryScanner> logger)
        {
            _jellyfinApiClient = jellyfinApiClient;
            _mediaSourceRepository = mediaSourceRepository;
            _televisionRepository = televisionRepository;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
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

            // List<JellyfinPathReplacement> pathReplacements = await _mediaSourceRepository
            //     .GetJellyfinPathReplacements(library.MediaSourceId);

            Either<BaseError, List<JellyfinShow>> maybeShows = await _jellyfinApiClient.GetShowLibraryItems(
                address,
                apiKey,
                library.MediaSourceId,
                library.ItemId);

            await maybeShows.Match(
                shows => ProcessShows(address, apiKey, library, ffprobePath, existingShows, shows),
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
            List<JellyfinItemEtag> existingShows,
            List<JellyfinShow> shows)
        {
            foreach (JellyfinShow incoming in shows)
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

                        await _televisionRepository.Update(incoming);
                        await _searchIndex.UpdateItems(
                            _searchRepository,
                            new List<MediaItem> { incoming });
                    },
                    async () =>
                    {
                        changed = true;
                        incoming.LibraryPathId = library.Paths.Head().Id;

                        _logger.LogDebug("INSERT: Item id is new for show {Show}", incoming.ShowMetadata.Head().Title);

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
                        seasons => ProcessSeasons(
                            address,
                            apiKey,
                            library,
                            ffprobePath,
                            incoming,
                            existingSeasons,
                            seasons),
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
                        await _searchIndex.UpdateItems(
                            _searchRepository,
                            new List<MediaItem> { incoming });
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

                        if (await _televisionRepository.AddSeason(incoming))
                        {
                            await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { incoming });
                        }
                    });
            }
        }
    }
}
