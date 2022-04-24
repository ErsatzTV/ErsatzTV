﻿using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using ErsatzTV.Core.Metadata;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Plex;

public class PlexMovieLibraryScanner : PlexLibraryScanner, IPlexMovieLibraryScanner
{
    private readonly ILocalFileSystem _localFileSystem;
    private readonly ILocalStatisticsProvider _localStatisticsProvider;
    private readonly ILocalSubtitlesProvider _localSubtitlesProvider;
    private readonly ILogger<PlexMovieLibraryScanner> _logger;
    private readonly IMediaSourceRepository _mediaSourceRepository;
    private readonly IMediator _mediator;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IPlexMovieRepository _plexMovieRepository;
    private readonly IPlexPathReplacementService _plexPathReplacementService;
    private readonly IPlexServerApiClient _plexServerApiClient;
    private readonly ISearchIndex _searchIndex;
    private readonly ISearchRepository _searchRepository;

    public PlexMovieLibraryScanner(
        IPlexServerApiClient plexServerApiClient,
        IMovieRepository movieRepository,
        IMetadataRepository metadataRepository,
        ISearchIndex searchIndex,
        ISearchRepository searchRepository,
        IMediator mediator,
        IMediaSourceRepository mediaSourceRepository,
        IPlexMovieRepository plexMovieRepository,
        IPlexPathReplacementService plexPathReplacementService,
        ILocalFileSystem localFileSystem,
        ILocalStatisticsProvider localStatisticsProvider,
        ILocalSubtitlesProvider localSubtitlesProvider,
        ILogger<PlexMovieLibraryScanner> logger)
        : base(metadataRepository, logger)
    {
        _plexServerApiClient = plexServerApiClient;
        _movieRepository = movieRepository;
        _metadataRepository = metadataRepository;
        _searchIndex = searchIndex;
        _searchRepository = searchRepository;
        _mediator = mediator;
        _mediaSourceRepository = mediaSourceRepository;
        _plexMovieRepository = plexMovieRepository;
        _plexPathReplacementService = plexPathReplacementService;
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
            Either<BaseError, List<PlexMovie>> entries = await _plexServerApiClient.GetMovieLibraryContents(
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
        List<PlexMovie> movieEntries,
        CancellationToken cancellationToken)
    {
        List<PlexItemEtag> existingMovies = await _movieRepository.GetExistingPlexMovies(library);

        List<PlexPathReplacement> pathReplacements = await _mediaSourceRepository
            .GetPlexPathReplacements(library.MediaSourceId);

        foreach (PlexMovie incoming in movieEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanCanceled();
            }

            decimal percentCompletion = (decimal)movieEntries.IndexOf(incoming) / movieEntries.Count;
            await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion), cancellationToken);

            if (await ShouldScanItem(library, pathReplacements, existingMovies, incoming, deepScan) == false)
            {
                continue;
            }

            // TODO: figure out how to rebuild playlists
            Either<BaseError, MediaItemScanResult<PlexMovie>> maybeMovie = await _movieRepository
                .GetOrAdd(library, incoming)
                .BindT(
                    existing => UpdateStatistics(pathReplacements, existing, incoming, ffmpegPath, ffprobePath))
                .BindT(existing => UpdateMetadata(existing, incoming, library, connection, token))
                .BindT(existing => UpdateSubtitles(pathReplacements, existing, incoming))
                .BindT(existing => UpdateArtwork(existing, incoming));

            if (maybeMovie.IsLeft)
            {
                foreach (BaseError error in maybeMovie.LeftToSeq())
                {
                    _logger.LogWarning(
                        "Error processing plex movie at {Key}: {Error}",
                        incoming.Key,
                        error.Value);
                }

                continue;
            }

            foreach (MediaItemScanResult<PlexMovie> result in maybeMovie.RightToSeq())
            {
                await _movieRepository.SetPlexEtag(result.Item, incoming.Etag);

                string plexPath = incoming.MediaVersions.Head().MediaFiles.Head().Path;

                string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                    pathReplacements,
                    plexPath,
                    false);

                if (_localFileSystem.FileExists(localPath))
                {
                    await _plexMovieRepository.FlagNormal(library, result.Item);
                }
                else
                {
                    await _plexMovieRepository.FlagUnavailable(library, result.Item);
                }

                if (result.IsAdded)
                {
                    await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                }
                else if (result.IsUpdated)
                {
                    await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                }
            }
        }

        // trash items that are no longer present on the media server
        var fileNotFoundKeys = existingMovies.Map(m => m.Key).Except(movieEntries.Map(m => m.Key)).ToList();
        List<int> ids = await _plexMovieRepository.FlagFileNotFound(library, fileNotFoundKeys);
        await _searchIndex.RebuildItems(_searchRepository, ids);

        await _mediator.Publish(new LibraryScanProgress(library.Id, 0), cancellationToken);

        return Unit.Default;
    }

    private async Task<bool> ShouldScanItem(
        PlexLibrary library,
        List<PlexPathReplacement> pathReplacements,
        List<PlexItemEtag> existingMovies,
        PlexMovie incoming,
        bool deepScan)
    {
        // deep scan will pull every movie individually from the plex api
        if (!deepScan)
        {
            Option<PlexItemEtag> maybeExisting = existingMovies.Find(ie => ie.Key == incoming.Key);
            string existingEtag = await maybeExisting
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
            else if (existingEtag == incoming.Etag)
            {
                if (!_localFileSystem.FileExists(localPath))
                {
                    await _plexMovieRepository.FlagUnavailable(library, incoming);
                }

                // _logger.LogDebug("NOOP: etag has not changed for plex movie with key {Key}", incoming.Key);
                return false;
            }

            _logger.LogDebug(
                "UPDATE: Etag has changed for movie {Movie}",
                incoming.MovieMetadata.Head().Title);
        }

        return true;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateStatistics(
        List<PlexPathReplacement> pathReplacements,
        MediaItemScanResult<PlexMovie> result,
        PlexMovie incoming,
        string ffmpegPath,
        string ffprobePath)
    {
        PlexMovie existing = result.Item;
        MediaVersion existingVersion = existing.MediaVersions.Head();
        MediaVersion incomingVersion = incoming.MediaVersions.Head();

        if (result.IsAdded || existing.Etag != incoming.Etag || existingVersion.Streams.Count == 0)
        {
            foreach (MediaFile incomingFile in incomingVersion.MediaFiles.HeadOrNone())
            {
                foreach (MediaFile existingFile in existingVersion.MediaFiles.HeadOrNone())
                {
                    if (incomingFile.Path != existingFile.Path)
                    {
                        _logger.LogDebug(
                            "Plex movie has moved from {OldPath} to {NewPath}",
                            existingFile.Path,
                            incomingFile.Path);

                        existingFile.Path = incomingFile.Path;

                        await _movieRepository.UpdatePath(existingFile.Id, incomingFile.Path);
                    }
                }
            }

            string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            // only refresh statistics if the file exists
            if (_localFileSystem.FileExists(localPath))
            {
                _logger.LogDebug("Refreshing {Attribute} for {Path}", "Statistics", localPath);
                Either<BaseError, bool> refreshResult =
                    await _localStatisticsProvider.RefreshStatistics(ffmpegPath, ffprobePath, existing, localPath);

                await refreshResult.Match(
                    async _ =>
                    {
                        foreach (MediaItem updated in await _searchRepository.GetItemToIndex(incoming.Id))
                        {
                            await _searchIndex.UpdateItems(
                                _searchRepository,
                                new List<MediaItem> { updated });
                        }

                        await _metadataRepository.UpdatePlexStatistics(existingVersion.Id, incomingVersion);
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
        }

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateMetadata(
        MediaItemScanResult<PlexMovie> result,
        PlexMovie incoming,
        PlexLibrary library,
        PlexConnection connection,
        PlexServerAuthToken token)
    {
        PlexMovie existing = result.Item;
        MovieMetadata existingMetadata = existing.MovieMetadata.Head();

        _logger.LogDebug(
            "Refreshing {Attribute} for {Title}",
            "Plex Metadata",
            existing.MovieMetadata.Head().Title);

        Either<BaseError, MovieMetadata> maybeMetadata =
            await _plexServerApiClient.GetMovieMetadata(
                library,
                incoming.Key.Split("/").Last(),
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
                    if (await _movieRepository.AddGenre(existingMetadata, genre))
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
                    if (await _movieRepository.AddStudio(existingMetadata, studio))
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
                    if (await _movieRepository.AddActor(existingMetadata, actor))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Director director in existingMetadata.Directors
                             .Filter(g => fullMetadata.Directors.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existingMetadata.Directors.Remove(director);
                    if (await _metadataRepository.RemoveDirector(director))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Director director in fullMetadata.Directors
                             .Filter(g => existingMetadata.Directors.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existingMetadata.Directors.Add(director);
                    if (await _movieRepository.AddDirector(existingMetadata, director))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Writer writer in existingMetadata.Writers
                             .Filter(g => fullMetadata.Writers.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existingMetadata.Writers.Remove(writer);
                    if (await _metadataRepository.RemoveWriter(writer))
                    {
                        result.IsUpdated = true;
                    }
                }

                foreach (Writer writer in fullMetadata.Writers
                             .Filter(g => existingMetadata.Writers.All(g2 => g2.Name != g.Name))
                             .ToList())
                {
                    existingMetadata.Writers.Add(writer);
                    if (await _movieRepository.AddWriter(existingMetadata, writer))
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
                    if (await _movieRepository.AddTag(existingMetadata, tag))
                    {
                        result.IsUpdated = true;
                    }
                }

                if (fullMetadata.SortTitle != existingMetadata.SortTitle)
                {
                    existingMetadata.SortTitle = fullMetadata.SortTitle;
                    if (await _movieRepository.UpdateSortTitle(existingMetadata))
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

        // TODO: update other metadata?

        return result;
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateSubtitles(
        List<PlexPathReplacement> pathReplacements,
        MediaItemScanResult<PlexMovie> result,
        PlexMovie incoming)
    {
        try
        {
            string localPath = _plexPathReplacementService.GetReplacementPlexPath(
                pathReplacements,
                incoming.MediaVersions.Head().MediaFiles.Head().Path,
                false);

            await _localSubtitlesProvider.UpdateSubtitles(result.Item, localPath, false);

            return result;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateArtwork(
        MediaItemScanResult<PlexMovie> result,
        PlexMovie incoming)
    {
        PlexMovie existing = result.Item;
        MovieMetadata existingMetadata = existing.MovieMetadata.Head();
        MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

        bool poster = await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
        bool fanArt = await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
        if (poster || fanArt)
        {
            await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
        }

        return result;
    }
}
