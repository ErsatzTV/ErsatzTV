using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Seq = LanguageExt.Seq;

namespace ErsatzTV.Core.Metadata
{
    public class MovieFolderScanner : LocalFolderScanner, IMovieFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILogger<MovieFolderScanner> _logger;
        private readonly IMovieRepository _movieRepository;

        public MovieFolderScanner(
            ILocalFileSystem localFileSystem,
            IMovieRepository movieRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IImageCache imageCache,
            ILogger<MovieFolderScanner> logger)
            : base(localFileSystem, localStatisticsProvider, imageCache, logger)
        {
            _localFileSystem = localFileSystem;
            _movieRepository = movieRepository;
            _localMetadataProvider = localMetadataProvider;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanFolder(LocalMediaSource localMediaSource, string ffprobePath)
        {
            if (!_localFileSystem.IsMediaSourceAccessible(localMediaSource))
            {
                return new MediaSourceInaccessible();
            }

            var folderQueue = new Queue<string>();
            foreach (string folder in _localFileSystem.ListSubdirectories(localMediaSource.Folder).OrderBy(identity))
            {
                folderQueue.Enqueue(folder);
            }

            while (folderQueue.Count > 0)
            {
                string movieFolder = folderQueue.Dequeue();

                var allFiles = _localFileSystem.ListFiles(movieFolder)
                    .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                    .Filter(
                        f => !ExtraFiles.Any(
                            e => Path.GetFileNameWithoutExtension(f).EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (allFiles.Count == 0)
                {
                    foreach (string subdirectory in _localFileSystem.ListSubdirectories(movieFolder).OrderBy(identity))
                    {
                        folderQueue.Enqueue(subdirectory);
                    }

                    continue;
                }

                foreach (string file in allFiles.OrderBy(identity))
                {
                    // TODO: figure out how to rebuild playlists
                    Either<BaseError, MovieMediaItem> x = await _movieRepository.GetOrAdd(localMediaSource.Id, file);

                    Either<BaseError, MovieMediaItem> maybeMovie = await x.AsTask()
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
                            (movie.Metadata.LastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(nfoFile))
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
                            (movie.PosterLastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            await SavePosterToDisk(movie, posterFile, _movieRepository.Update, 440);
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
            Option<string> result = possibleMoviePosters.Filter(p => _localFileSystem.FileExists(p)).HeadOrNone();
            return result;
        }
    }
}
