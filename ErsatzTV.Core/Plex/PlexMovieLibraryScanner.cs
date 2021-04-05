using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Plex;
using ErsatzTV.Core.Interfaces.Repositories;
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

        public PlexMovieLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            IMovieRepository movieRepository,
            IMetadataRepository metadataRepository,
            IMediator mediator,
            ILogger<PlexMovieLibraryScanner> logger)
            : base(metadataRepository, logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _movieRepository = movieRepository;
            _metadataRepository = metadataRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary,
            Func<List<MediaItem>, ValueTask> addToSearchIndex,
            Func<List<int>, ValueTask> removeFromSearchIndex)
        {
            Either<BaseError, List<PlexMovie>> entries = await _plexServerApiClient.GetMovieLibraryContents(
                plexMediaSourceLibrary,
                connection,
                token);

            await entries.Match(
                async movieEntries =>
                {
                    foreach (PlexMovie incoming in movieEntries)
                    {
                        decimal percentCompletion = (decimal) movieEntries.IndexOf(incoming) / movieEntries.Count;
                        await _mediator.Publish(new LibraryScanProgress(plexMediaSourceLibrary.Id, percentCompletion));

                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, MediaItemScanResult<PlexMovie>> maybeMovie = await _movieRepository
                            .GetOrAdd(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeMovie.Match(
                            async result => await addToSearchIndex(new List<MediaItem> { result.Item }),
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
                    List<int> ids = await _movieRepository.RemoveMissingPlexMovies(plexMediaSourceLibrary, movieKeys);
                    await removeFromSearchIndex(ids);

                    await _mediator.Publish(new LibraryScanProgress(plexMediaSourceLibrary.Id, 0));
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
            PlexMovie incoming)
        {
            PlexMovie existing = result.Item;
            MovieMetadata existingMetadata = existing.MovieMetadata.Head();
            MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                _logger.LogDebug(
                    "Refreshing {Attribute} from {Path}",
                    "Plex Metadata",
                    existing.MediaVersions.Head().MediaFiles.Head().Path);

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
                    if (await _movieRepository.AddGenre(existingMetadata, genre))
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
                    if (await _movieRepository.AddStudio(existingMetadata, studio))
                    {
                        result.IsUpdated = true;
                    }
                }

                if (incomingMetadata.SortTitle != existingMetadata.SortTitle)
                {
                    existingMetadata.SortTitle = incomingMetadata.SortTitle;
                    if (await _movieRepository.UpdateSortTitle(existingMetadata))
                    {
                        result.IsUpdated = true;
                    }
                }

                await _metadataRepository.MarkAsUpdated(existingMetadata, incomingMetadata.DateUpdated);

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
