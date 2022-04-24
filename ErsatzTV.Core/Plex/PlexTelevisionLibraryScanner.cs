using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using LanguageExt.UnsafeValueAccess;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex;

public class PlexTelevisionLibraryScanner : PlexLibraryScanner, IPlexTelevisionLibraryScanner
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<PlexTelevisionLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly IPlexServerApiClient _plexServerApiClient;
    private readonly IPlexTelevisionRepository _plexTelevisionRepository;
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
        IPlexTelevisionRepository plexTelevisionRepository,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
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
        _plexTelevisionRepository = plexTelevisionRepository;
        _localFileSystem = localFileSystem;
        _localStatisticsProvider = localStatisticsProvider;
        _localSubtitlesProvider = localSubtitlesProvider;
        _logger = logger;
    }

    public async Task<Either<BaseError, Unit>> ScanLibrary(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        try
        {
            Either<BaseError, List<PlexShow>> entries = await _plexServerApiClient.GetShowLibraryContents(
                library,
                connection,
                token);

            foreach (BaseError error in entries.LeftToSeq())
            {
                return error;
            }

            return await ScanLibrary(
                connection,
                token,
                library,
                ffmpegPath,
                ffprobePath,
                deepScan,
                entries.RightToSeq().Flatten().ToList(),
                cancellationToken);
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            return new ScanCanceled();
        }
        finally
        {
            // always commit the search index to prevent corruption
            _searchIndex.Commit();
        }
    }

    private async Task<Either<BaseError, Unit>> ScanLibrary(
        PlexConnection connection,
        PlexServerAuthToken token,
        PlexLibrary library,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        List<PlexShow> showEntries,
        CancellationToken cancellationToken)
    {
        List<PlexItemEtag> existingShows = await _plexTelevisionRepository.GetExistingPlexShows(library);

        List<PlexPathReplacement> pathReplacements = await _mediaSourceRepository
            .GetPlexPathReplacements(library.MediaSourceId);

        foreach (PlexShow incoming in showEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)showEntries.IndexOf(incoming) / showEntries.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            // TODO: figure out how to rebuild playlists
            Either<BaseError, MediaItemScanResult<PlexShow>> maybeShow = await _televisionRepository
                .GetOrAddPlexShow(library, incoming)
                .BindT(existing => UpdateMetadata(existing, incoming, library, connection, token, deepScan))
                .BindT(existing => UpdateArtwork(existing, incoming));

            if (maybeShow.IsLeft)
            {
                foreach (BaseError error in maybeShow.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing plex show at {Key}: {Error}",
                        incoming.Key,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<PlexShow> result in maybeShow.RightToSeq())
            {
                Either<BaseError, Unit> scanResult = await ScanSeasons(
                    library,
                    pathReplacements,
                    result.Item,
                    connection,
                    token,
                    ffmpegPath,
                    ffprobePath,
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await _plexTelevisionRepository.SetPlexEtag(result.Item, incoming.Etag);

                // TODO: if any seasons are unavailable or not found, flag show as unavailable/not found

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
            }
        }

        // trash items that are no longer present on the media server
        var fileNotFoundKeys = existingShows.Map(m => m.Key).Except(showEntries.Map(m => m.Key)).ToList();
        List<int> ids = await _plexTelevisionRepository.FlagFileNotFoundShows(library, fileNotFoundKeys);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);

        return Unit.Default;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexShow>>> UpdateMetadata(
        MediaItemScanResult<PlexShow> result,
        PlexShow incoming,
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token,
        bool deepScan)
    {
        PlexShow existing = result.Item;
        ShowMetadata existingMetadata = existing.ShowMetadata.Head();

        if (result.IsAdded || existing.Etag != incoming.Etag || deepScan)
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

                    foreach (Tag tag in existingMetadata.Tags
                                 .Filter(g => fullMetadata.Tags.All(g2 => g2.Name != g.Name))
                                 .ToList())
                    {
                        existingMetadata.Tags.Remove(tag);
                        if (await _metadataRepository.RemoveTag(tag))
                        {
                            result.IsUpdated = true;
                        }
                    }

                    foreach (Tag tag in fullMetadata.Tags
                                 .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                                 .ToList())
                    {
                        existingMetadata.Tags.Add(tag);
                        if (await _televisionRepository.AddTag(existingMetadata, tag))
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

        bool poster = await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
        bool fanArt = await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
        if (poster || fanArt)
        {
            await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
        }

        return result;
    }

    private async Task<Either<BaseError, Unit>> ScanSeasons(
        PlexLibrary library,
        List<PlexPathReplacement> pathReplacements,
        PlexShow show,
        PlexConnection connection,
        PlexServerAuthToken token,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<PlexItemEtag> existingSeasons = await _plexTelevisionRepository.GetExistingPlexSeasons(library, show);

        Either<BaseError, List<PlexSeason>> entries = await _plexServerApiClient.GetShowSeasons(
            library,
            show,
            connection,
            token);

        foreach (BaseError error in entries.LeftToSeq())
        {
            return error;
        }

        var seasonEntries = entries.RightToSeq().Flatten().ToList();
        foreach (PlexSeason incoming in seasonEntries)
        {
            incoming.ShowId = show.Id;

            // TODO: figure out how to rebuild playlists
            Either<BaseError, PlexSeason> maybeSeason = await _televisionRepository
                .GetOrAddPlexSeason(library, incoming)
                .BindT(existing => UpdateMetadataAndArtwork(existing, incoming, deepScan));

            foreach (BaseError error in maybeSeason.LeftToSeq())
            {
                _logger.LogWarning(
                    "Error processing plex season at {Key}: {Error}",
                    incoming.Key,
                    error.Value);

                return error;
            }

            foreach (PlexSeason season in maybeSeason.RightToSeq())
            {
                Either<BaseError, Unit> scanResult = await ScanEpisodes(
                    library,
                    pathReplacements,
                    season,
                    connection,
                    token,
                    ffmpegPath,
                    ffprobePath,
                    deepScan,
                    cancellationToken);

                foreach (ScanCanceled error in scanResult.LeftToSeq().OfType<ScanCanceled>())
                {
                    return error;
                }

                await _plexTelevisionRepository.SetPlexEtag(season, incoming.Etag);

                season.Show = show;

                // TODO: if any seasons are unavailable or not found, flag show as unavailable/not found

                await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { season });
            }
        }

        var fileNotFoundKeys = existingSeasons.Map(m => m.Key).Except(seasonEntries.Map(m => m.Key)).ToList();
        List<int> ids = await _plexTelevisionRepository.FlagFileNotFoundSeasons(library, fileNotFoundKeys);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        return Unit.Default;
    }

    private async Task<Either<BaseError, PlexSeason>> UpdateMetadataAndArtwork(
        PlexSeason existing,
        PlexSeason incoming,
        bool deepScan)
    {
        SeasonMetadata existingMetadata = existing.SeasonMetadata.Head();
        SeasonMetadata incomingMetadata = incoming.SeasonMetadata.Head();

        if (existing.Etag != incoming.Etag || deepScan)
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

            foreach (Tag tag in existingMetadata.Tags
                         .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                         .ToList())
            {
                existingMetadata.Tags.Remove(tag);
                await _metadataRepository.RemoveTag(tag);
            }

            foreach (Tag tag in incomingMetadata.Tags
                         .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                         .ToList())
            {
                existingMetadata.Tags.Add(tag);
                await _televisionRepository.AddTag(existingMetadata, tag);
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
        PlexServerAuthToken token,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan,
        CancellationToken cancellationToken)
    {
        List<PlexItemEtag> existingEpisodes = await _plexTelevisionRepository.GetExistingPlexEpisodes(library, season);

        Either<BaseError, List<PlexEpisode>> entries = await _plexServerApiClient.GetSeasonEpisodes(
            library,
            season,
            connection,
            token);

        foreach (BaseError error in entries.LeftToSeq())
        {
            return error;
        }

        var episodeEntries = entries.RightToSeq().Flatten().ToList();
        foreach (PlexEpisode incoming in episodeEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            if (await ShouldScanItem(library, pathReplacements, existingEpisodes, incoming, deepScan) == false)
            {
                continue;
            }

            incoming.SeasonId = season.Id;

            // TODO: figure out how to rebuild playlists
            Either<BaseError, MediaItemScanResult<PlexEpisode>> maybeEpisode = await _televisionRepository
                .GetOrAddPlexEpisode(library, incoming)
                .BindT(existing => UpdateMetadata(existing, incoming))
                .BindT(
                    existing => UpdateStatistics(
                        pathReplacements,
                        existing,
                        incoming,
                        library,
                        connection,
                        token,
                        ffmpegPath,
                        ffprobePath,
                        deepScan))
                .BindT(existing => UpdateSubtitles(pathReplacements, existing, incoming))
                .BindT(existing => UpdateArtwork(existing, incoming));

            foreach (BaseError error in maybeEpisode.LeftToSeq())
            {
                switch (error)
                {
                    case ScanCanceled:
                        return error;
                    default:
                        _logger.LogWarning(
                            "Error processing plex episode at {Key}: {Error}",
                            incoming.Key,
                            error.Value);
                        break;
                }
            }

            foreach (MediaItemScanResult<PlexEpisode> result in maybeEpisode.RightToSeq())
            {
                await _plexTelevisionRepository.SetPlexEtag(result.Item, incoming.Etag);

                string plexPath = incoming.MediaVersions.Head().MediaFiles.Head().Path;

                string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                    pathReplacements,
                    plexPath,
                    false);

                if (_localFileSystem.FileExists(localPath))
                {
                    await _plexTelevisionRepository.FlagNormal(library, result.Item);
                }
                else
                {
                    await _plexTelevisionRepository.FlagUnavailable(library, result.Item);
                }

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        var fileNotFoundKeys = existingEpisodes.Map(m => m.Key).Except(episodeEntries.Map(m => m.Key)).ToList();
        List<int> ids = await _plexTelevisionRepository.FlagFileNotFoundEpisodes(library, fileNotFoundKeys);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        _searchIndex.Commit();

        return Unit.Default;
    }

    private async Task<bool> ShouldScanItem(
        PlexLibrary library,
        List<PlexPathReplacement> pathReplacements,
        List<PlexItemEtag> existingEpisodes,
        PlexEpisode incoming,
        bool deepScan)
    {
        // deep scan will pull every episode individually from the plex api
        if (!deepScan)
        {
            Option<PlexItemEtag> maybeExisting = existingEpisodes.Find(ie => ie.Key == incoming.Key);
            string existingTag = await maybeExisting
                .Map(e => e.Etag ?? string.Empty)
                .IfNoneAsync(string.Empty);
            MediaItemState existingState = await maybeExisting
                .Map(e => e.State)
                .IfNoneAsync(MediaItemState.Normal);

            string plexPath = incoming.MediaVersions.Head().MediaFiles.Head().Path;

            string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                plexPath,
                false);

            // if media is unavailable, only scan if file now exists
            if (existingState == MediaItemState.Unavailable)
            {
                if (!_localFileSystem.FileExists(localPath))
                {
                    return false;
                }
            }
            else if (existingTag == incoming.Etag)
            {
                if (!_localFileSystem.FileExists(localPath))
                {
                    foreach (int id in await _plexTelevisionRepository.FlagUnavailable(library, incoming))
                    {
                        await _searchIndex.RebuildItems(_searchRepository, new List<int> { id });
                    }
                }

                // _logger.LogDebug("NOOP: etag has not changed for plex episode with key {Key}", incoming.Key);
                return false;
            }

            // _logger.LogDebug(
            //     "UPDATE: Etag has changed for episode {Episode}",
            //     $"s{season.SeasonNumber}e{incoming.EpisodeMetadata.Head().EpisodeNumber}");
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateMetadata(
        MediaItemScanResult<PlexEpisode> result,
        PlexEpisode incoming)
    {
        PlexEpisode existing = result.Item;

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

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateStatistics(
        List<PlexPathReplacement> pathReplacements,
        MediaItemScanResult<PlexEpisode> result,
        PlexEpisode incoming,
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token,
        string ffmpegPath,
        string ffprobePath,
        bool deepScan)
    {
        PlexEpisode existing = result.Item;
        MediaVersion existingVersion = existing.MediaVersions.Head();
        MediaVersion incomingVersion = incoming.MediaVersions.Head();

        if (result.IsAdded || existing.Etag != incoming.Etag || deepScan || existingVersion.Streams.Count == 0)
        {
            foreach (MediaFile incomingFile in incomingVersion.MediaFiles.HeadOrNone())
            {
                foreach (MediaFile existingFile in existingVersion.MediaFiles.HeadOrNone())
                {
                    if (incomingFile.Path != existingFile.Path)
                    {
                        _logger.LogDebug(
                            "Plex episode has moved from {OldPath} to {NewPath}",
                            existingFile.Path,
                            incomingFile.Path);

                        existingFile.Path = incomingFile.Path;

                        await _televisionRepository.UpdatePath(existingFile.Id, incomingFile.Path);
                    }
                }
            }

            Either<BaseError, bool> refreshResult = true;

            string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            if ((existing.Etag != incoming.Etag || existingVersion.Streams.Count == 0) &&
                _localFileSystem.FileExists(localPath))
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                refreshResult = await _localStatisticsProvider.RefreshStatistics(
                    ffmpegPath,
                    ffprobePath,
                    existing,
                    localPath);
            }

            await refreshResult.Match(
                async _ =>
                {
                    foreach (MediaItem updated in await _searchRepository.GetItemToIndex(incoming.Id))
                    {
                        await _searchIndex.UpdateItems(
                            _searchRepository,
                            new List<MediaItem> { updated });
                    }

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

                                foreach (Tag tag in existingMetadata.Tags
                                             .Filter(g => incomingMetadata.Tags.All(g2 => g2.Name != g.Name))
                                             .ToList())
                                {
                                    existingMetadata.Tags.Remove(tag);
                                    await _metadataRepository.RemoveTag(tag);
                                }

                                foreach (Tag tag in incomingMetadata.Tags
                                             .Filter(g => existingMetadata.Tags.All(g2 => g2.Name != g.Name))
                                             .ToList())
                                {
                                    existingMetadata.Tags.Add(tag);
                                    await _televisionRepository.AddTag(existingMetadata, tag);
                                }
                            }

                            existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio;
                            existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                            existingVersion.DateUpdated = mediaVersion.DateUpdated;

                            await _metadataRepository.UpdatePlexStatistics(existingVersion.Id, mediaVersion);
                        },
                        _ => Task.CompletedTask);
                },
                error =>
                {
                    _logger.LogWarning(
                        "Unable to refresh {Attribute} for media item {Path}. Error: {Error}",
                        "Statistics",
                        localPath,
                        error.Value);

                    return Task.CompletedTask;
                });
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateSubtitles(
        List<PlexPathReplacement> pathReplacements,
        MediaItemScanResult<PlexEpisode> result,
        PlexEpisode incoming)
    {
        try
        {
            PlexEpisode existing = result.Item;

            string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            await _localSubtitlesProvider.UpdateSubtitles(existing, localPath, false);

            return result;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>> UpdateArtwork(
        MediaItemScanResult<PlexEpisode> result,
        PlexEpisode incoming)
    {
        PlexEpisode existing = result.Item;
        foreach (EpisodeMetadata incomingMetadata in incoming.EpisodeMetadata)
        {
            Option<EpisodeMetadata> maybeExistingMetadata = existing.EpisodeMetadata
                .Find(em => em.EpisodeNumber == incomingMetadata.EpisodeNumber);
            if (maybeExistingMetadata.IsSome)
            {
                EpisodeMetadata existingMetadata = maybeExistingMetadata.ValueUnsafe();
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Thumbnail);
                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }
        }

        return result;
    }
}
