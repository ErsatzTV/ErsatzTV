using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Metadata
{
    public class MovieFolderScanner : LocalFolderScanner, IMovieFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILocalPosterProvider _localPosterProvider;
        private readonly ILogger<MovieFolderScanner> _logger;
        private readonly IMovieRepository _movieRepository;

        public MovieFolderScanner(
            ILocalFileSystem localFileSystem,
            IMovieRepository movieRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            ILocalPosterProvider localPosterProvider,
            ILogger<MovieFolderScanner> logger)
            : base(localFileSystem, localStatisticsProvider, logger)
        {
            _localFileSystem = localFileSystem;
            _movieRepository = movieRepository;
            _localMetadataProvider = localMetadataProvider;
            _localPosterProvider = localPosterProvider;
            _logger = logger;
        }

        public async Task<Unit> ScanFolder(LocalMediaSource localMediaSource, string ffprobePath)
        {
            if (!_localFileSystem.IsMediaSourceAccessible(localMediaSource))
            {
                _logger.LogWarning(
                    "Media source is not accessible or missing; skipping scan of {Folder}",
                    localMediaSource.Folder);
                return Unit.Default;
            }

            foreach (string movieFolder in _localFileSystem.ListSubdirectories(localMediaSource.Folder))
            {
                foreach (string file in _localFileSystem.ListFiles(movieFolder)
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f))))
                {
                    // TODO: figure out how to rebuild playlists
                    Either<BaseError, MovieMediaItem> maybeMovie = await _movieRepository
                        .GetOrAdd(localMediaSource.Id, file)
                        .BindT(movie => UpdateStatistics(movie, ffprobePath).MapT(_ => movie))
                        .BindT(UpdateMetadata)
                        .BindT(UpdatePoster);

                    maybeMovie.IfLeft(
                        error => _logger.LogWarning("Error processing movie at {Path}: {Error}", file, error.Value));
                }
            }

            return Unit.Default;
        }

        private async Task<Either<BaseError, MovieMediaItem>> UpdateMetadata(MovieMediaItem movie)
        {
            try
            {
                await LocateNfoFile(movie).Match(
                    async nfoFile =>
                    {
                        if (movie.Metadata == null || movie.Metadata.Source == MetadataSource.Fallback ||
                            movie.Metadata.LastWriteTime < _localFileSystem.GetLastWriteTime(nfoFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            await _localMetadataProvider.RefreshSidecarMetadata(movie, nfoFile);
                        }
                    },
                    async () =>
                    {
                        if (movie.Metadata == null)
                        {
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", movie.Path);
                            await _localMetadataProvider.RefreshFallbackMetadata(movie);
                        }
                    });

                return movie;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, MovieMediaItem>> UpdatePoster(MovieMediaItem movie)
        {
            try
            {
                await LocatePoster(movie).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(movie.Poster) ||
                            movie.PosterLastWriteTime < _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            await _localPosterProvider.SavePosterToDisk(movie, posterFile);
                        }
                    });

                return movie;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private Option<string> LocateNfoFile(MovieMediaItem movie)
        {
            string movieAsNfo = Path.ChangeExtension(movie.Path, "nfo");
            string movieNfo = Path.Combine(Path.GetDirectoryName(movie.Path) ?? string.Empty, "movie.nfo");
            return Seq.create(movieAsNfo, movieNfo)
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private Option<string> LocatePoster(MovieMediaItem movie)
        {
            string folder = Path.GetDirectoryName(movie.Path) ?? string.Empty;
            IEnumerable<string> possibleMoviePosters = ImageFileExtensions.Collect(
                    ext => new[] { $"poster.{ext}", Path.GetFileNameWithoutExtension(movie.Path) + $"-poster.{ext}" })
                .Map(f => Path.Combine(folder, f));
            return possibleMoviePosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
        }
    }
}
