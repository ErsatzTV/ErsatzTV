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
    public class PlexMovieLibraryScanner : PlexLibraryScanner, IPlexMovieLibraryScanner
    {
        private readonly ILogger<PlexMovieLibraryScanner> _logger;
        private readonly IMetadataRepository _metadataRepository;
        private readonly IMovieRepository _movieRepository;
        private readonly IPlexServerApiClient _plexServerApiClient;

        public PlexMovieLibraryScanner(
            IPlexServerApiClient plexServerApiClient,
            IMovieRepository movieRepository,
            IMetadataRepository metadataRepository,
            ILogger<PlexMovieLibraryScanner> logger)
            : base(metadataRepository)
        {
            _plexServerApiClient = plexServerApiClient;
            _movieRepository = movieRepository;
            _metadataRepository = metadataRepository;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanLibrary(
            PlexConnection connection,
            PlexServerAuthToken token,
            PlexLibrary plexMediaSourceLibrary)
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
                        // TODO: figure out how to rebuild playlists
                        Either<BaseError, PlexMovie> maybeMovie = await _movieRepository
                            .GetOrAdd(plexMediaSourceLibrary, incoming)
                            .BindT(existing => UpdateStatistics(existing, incoming, connection, token))
                            .BindT(existing => UpdateMetadata(existing, incoming))
                            .BindT(existing => UpdateArtwork(existing, incoming));

                        maybeMovie.IfLeft(
                            error => _logger.LogWarning(
                                "Error processing plex movie at {Key}: {Error}",
                                incoming.Key,
                                error.Value));
                    }

                    var movieKeys = movieEntries.Map(s => s.Key).ToList();
                    await _movieRepository.RemoveMissingPlexMovies(plexMediaSourceLibrary, movieKeys);
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
                    await _plexServerApiClient.GetStatistics(incoming.Key.Split("/").Last(), connection, token);

                await maybeStatistics.Match(
                    async mediaVersion =>
                    {
                        existingVersion.SampleAspectRatio = mediaVersion.SampleAspectRatio ?? "1:1";
                        existingVersion.VideoScanKind = mediaVersion.VideoScanKind;
                        existingVersion.DateUpdated = incomingVersion.DateUpdated;

                        await _metadataRepository.UpdateStatistics(existingVersion);
                    },
                    _ => Task.CompletedTask);
            }

            return Right<BaseError, PlexMovie>(existing);
        }

        private async Task<Either<BaseError, PlexMovie>> UpdateMetadata(PlexMovie existing, PlexMovie incoming)
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
                    await _metadataRepository.RemoveGenre(genre);
                }

                foreach (Genre genre in incomingMetadata.Genres
                    .Filter(g => existingMetadata.Genres.All(g2 => g2.Name != g.Name))
                    .ToList())
                {
                    existingMetadata.Genres.Add(genre);
                    await _movieRepository.AddGenre(existingMetadata, genre);
                }
            }

            return existing;
        }

        private async Task<Either<BaseError, PlexMovie>> UpdateArtwork(PlexMovie existing, PlexMovie incoming)
        {
            MovieMetadata existingMetadata = existing.MovieMetadata.Head();
            MovieMetadata incomingMetadata = incoming.MovieMetadata.Head();

            if (incomingMetadata.DateUpdated > existingMetadata.DateUpdated)
            {
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.Poster);
                await UpdateArtworkIfNeeded(existingMetadata, incomingMetadata, ArtworkKind.FanArt);
            }

            return existing;
        }
    }
}
