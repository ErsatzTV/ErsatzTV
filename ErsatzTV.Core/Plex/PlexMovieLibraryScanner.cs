﻿using System.Collections.Generic;
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
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Plex
{
    public class PlexMovieLibraryScanner : PlexLibraryScanner, IPlexMovieLibraryScanner
    {
        private readonly ILogger<PlexMovieLibraryScanner> _logger;
        private readonly IMediator _mediator;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IMovieRepository _movieRepository;
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
            ILogger<PlexMovieLibraryScanner> logger)
            : base(metadataRepository, logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _movieRepository = movieRepository;
            _metadataRepository = metadataRepository;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary library)
        {
            Either<BaseError, List<PlexMovie>> entries = await _plexServerApiClient.GetMovieLibraryContents(
                library,
                connection,
                token);

            await entries.Match(
                async movieEntries =>
                {
                    foreach (PlexMovie incoming in movieEntries)
                    {
                        decimal percentCompletion = (decimal) movieEntries.IndexOf(incoming) / movieEntries.Count;
                        await _mediator.Publish(new LibraryScanProgress(library.Id, percentCompletion));

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, MediaItemScanResult<PlexMovie>> maybeMovie = await _movieRepository
                            .GetOrAdd(library, incoming)
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                            .BindT(existing => UpdateMetadata(existing, incoming, library, connection, token))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeMovie.Match(
                            async result =>
                            {
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
                                    "Error processing plex movie at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
                    }

                    var movieKeys = movieEntries.Map(s => s.Key).ToList();
                    List<int> ids = await _movieRepository.RemoveMissingPlexMovies(library, movieKeys);
                    await _searchIndex.RemoveItems(ids);

                    await _mediator.Publish(new LibraryScanProgress(library.Id, 0));
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        library.Name,
                        error.Value);

                    return Task.CompletedTask;
                });

            _searchIndex.Commit();
            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateStatistics(
            MediaItemScanResult<PlexMovie> result,
            PlexMovie incoming,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            PlexMovie existing = result.Item;
            MediaVersion existingVersion = existing.MediaVersions.Head();
            MediaVersion incomingVersion = incoming.MediaVersions.Head();

            if (incomingVersion.DateUpdated > existingVersion.DateUpdated || !existingVersion.Streams.Any())
            {
                Either<BaseError, MediaVersion> maybeStatistics =
                    await _plexServerApiClient.GetStatistics(incoming.Key.Split("/").Last(), connection, token);

                await maybeStatistics.Match(
                    async mediaVersion =>
                    {
                        _logger.LogDebug(
                            "Refreshing {Attribute} from {Path}",
                            "Plex Statistics",
                            existingVersion.MediaFiles.Head().Path);

                        existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio;
                        existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                        existingVersion.DateUpdated = mediaVersion.DateUpdated;

                        await _metadataRepository.UpdatePlexStatistics(existingVersion.Id, mediaVersion);
                    },
                    _ => Task.CompletedTask);
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

            if (incoming.MovieMetadata.Head().DateUpdated > existingMetadata.DateUpdated)
            {
                _logger.LogDebug(
                    "Refreshing {Attribute} from {Path}",
                    "Plex Metadata",
                    existing.MediaVersions.Head().MediaFiles.Head().Path);

                Either<BaseError, MovieMetadata> maybeMetadata =
                    await _plexServerApiClient.GetMovieMetadata(
                        library,
                        incoming.Key.Split("/").Last(),
                        connection,
                        token);

                await maybeMetadata.Match(
                    async fullMetadata =>
                    {
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
                            .Filter(a => fullMetadata.Actors.All(a2 => a2.Name != a.Name))
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
            }

            return result;
        }

        private async Task<Either<BaseError, MediaItemScanResult<PlexMovie>>> UpdateArtwork(
            MediaItemScanResult<PlexMovie> result,
            PlexMovie incoming)
        {
            PlexMovie existing = result.Item;
            MovieMetadata existingMetadata = existing.MovieMetadata.Head();
            MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);
            }

            return result;
        }
    }
}
