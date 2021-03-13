﻿using System.Collections.Generic;
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
    public class PlexMovieLibraryScanner : IPlexMovieLibraryScanner
    {
        private readonly ILogger<PlexMovieLibraryScanner> _logger;
        private readonly IMovieRepository _movieRepository;
        private readonly IPlexServerApiClient _plexServerApiClient;

        public PlexMovieLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            IMovieRepository movieRepository,
            ILogger<PlexMovieLibraryScanner> logger)
        {
            _plexServerApiClient = plexServerApiClient;
            _movieRepository = movieRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary)
        {
            Either<BaseError, List<PlexMovie>> entries = await _plexServerApiClient.GetLibraryContents(
                plexMediaSourceLibrary,
                connection,
                token);

            await entries.Match(
                async movieEntries =>
                {
                    foreach (PlexMovie incoming in movieEntries)
                    {
                        // TODO: optimize dbcontext use here, do we need tracking? can we make partial updates with dapper?
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexMovie> maybeMovie = await _movieRepository
                            .GetOrAdd(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        await maybeMovie.Match(
                            async movie => await _movieRepository.Update(movie),
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing plex movie at {Key}: {Error}",
                                    incoming.Key,
                                    error.Value);
                                return Task.CompletedTask;
                            });
                    }
                },
                error =>
                {
                    _logger.LogWarning(
                        "Error synchronizing plex library {Path}: {Error}",
                        plexMediaSourceLibrary.Name,
                        error.Value);

                    return Task.CompletedTask;
                });

            // need plex media item model that can be used to lookup by unique id (metadata key?)

            return Unit.Default;
        }

        private async Task<Either<BaseError, PlexMovie>> UpdateStatistics(
            PlexMovie existing,
            PlexMovie incoming,
            PlexConnection connection,
            PlexServerAuthToken token)
        {
            MediaVersion existingVersion = existing.MediaVersions.Head();
            MediaVersion incomingVersion = incoming.MediaVersions.Head();

            if (incomingVersion.DateUpdated > existingVersion.DateUpdated ||
                string.IsNullOrWhiteSpace(existingVersion.SampleAspectRatio))
            {
                Either<BaseError, MediaVersion> maybeStatistics =
                    await _plexServerApiClient.GetStatistics(incoming, connection, token);

                maybeStatistics.IfRight(
                    mediaVersion =>
                    {
                        existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio ?? "1:1";
                        existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                        existingVersion.DateUpdated = incomingVersion.DateUpdated;
                    });
            }

            return Right<BaseError, PlexMovie>(existing);
        }

        private Task<Either<BaseError, PlexMovie>> UpdateMetadata(PlexMovie existing, PlexMovie incoming)
        {
            MovieMetadata existingMetadata = existing.MovieMetadata.Head();
            MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                foreach (Genre genre in existingMetadata.Genres
                    .Filter(g => incomingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Remove(genre);
                }

                foreach (Genre genre in incomingMetadata.Genres
                    .Filter(g => existingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Add(genre);
                }
            }

            return Right<BaseError, PlexMovie>(existing).AsTask();
        }

        private Task<Either<BaseError, PlexMovie>> UpdateArtwork(PlexMovie existing, PlexMovie incoming)
        {
            MovieMetadata existingMetadata = existing.MovieMetadata.Head();
            MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
            }

            return Right<BaseError, PlexMovie>(existing).AsTask();
        }

        private void UpdateArtworkIfNeeded(
            MovieMetadata existingMetadata,
            MovieMetadata incomingMetadata,
            ArtworkKind artworkKind)
        {
            Option<Artwork> maybeIncomingArtwork = Optional(incomingMetadata.Artwork).Flatten()
                .Find(a => a.ArtworkKind == artworkKind);

            maybeIncomingArtwork.Match(
                incomingArtwork =>
                {
                    Option<Artwork> maybeExistingArtwork = Optional(existingMetadata.Artwork).Flatten()
                        .Find(a => a.ArtworkKind == artworkKind);

                    maybeExistingArtwork.Match(
                        existingArtwork =>
                        {
                            existingArtwork.Path = incomingArtwork.Path;
                            existingArtwork.DateUpdated = incomingArtwork.DateUpdated;
                        },
                        () =>
                        {
                            existingMetadata.Artwork ??= new List<Artwork>();
                            existingMetadata.Artwork.Add(incomingArtwork);
                        });
                },
                () =>
                {
                    existingMetadata.Artwork ??= new List<Artwork>();
                    existingMetadata.Artwork.RemoveAll(a => a.ArtworkKind == artworkKind);
                });
        }
    }
}
