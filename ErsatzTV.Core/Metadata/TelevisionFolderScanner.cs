using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class TelevisionFolderScanner : LocalFolderScanner, ITelevisionFolderScanner
    {
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILogger<TelevisionFolderScanner> _logger;
        private readonly ITelevisionRepository _televisionRepository;

        public TelevisionFolderScanner(
            ILocalFileSystem localFileSystem,
            ITelevisionRepository televisionRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IImageCache imageCache,
            ILogger<TelevisionFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            imageCache,
            logger)
        {
            _localFileSystem = localFileSystem;
            _televisionRepository = televisionRepository;
            _localMetadataProvider = localMetadataProvider;
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

            var allShowFolders = _localFileSystem.ListSubdirectories(localMediaSource.Folder)
                .Filter(ShouldIncludeFolder)
                .ToList();

            foreach (string showFolder in allShowFolders)
            {
                Either<BaseError, TelevisionShow> maybeShow = await _televisionRepository
                    .GetOrAddShow(localMediaSource.Id, showFolder)
                    .BindT(UpdateMetadata)
                    .BindT(UpdatePoster);

                await maybeShow.Match(
                    show => ScanSeasons(localMediaSource, ffprobePath, show),
                    _ => Task.FromResult(Unit.Default));
            }

            List<TelevisionShow> removedShows =
                await _televisionRepository.FindRemovedShows(localMediaSource, allShowFolders);
            foreach (TelevisionShow show in removedShows)
            {
                _logger.LogDebug("Removing missing show at {Folder}", show.Path);
                await _televisionRepository.Delete(show);
            }

            return Unit.Default;
        }

        private async Task<Unit> ScanSeasons(LocalMediaSource localMediaSource, string ffprobePath, TelevisionShow show)
        {
            foreach (string seasonFolder in _localFileSystem.ListSubdirectories(show.Path).Filter(ShouldIncludeFolder))
            {
                Option<int> maybeSeasonNumber = SeasonNumberForFolder(seasonFolder);
                await maybeSeasonNumber.IfSomeAsync(
                    async seasonNumber =>
                    {
                        Either<BaseError, TelevisionSeason> maybeSeason = await _televisionRepository
                            .GetOrAddSeason(show, seasonFolder, seasonNumber)
                            .BindT(UpdatePoster);

                        await maybeSeason.Match(
                            season => ScanEpisodes(localMediaSource, ffprobePath, season),
                            _ => Task.FromResult(Unit.Default));
                    });
            }

            return Unit.Default;
        }

        private async Task<Unit> ScanEpisodes(
            LocalMediaSource localMediaSource,
            string ffprobePath,
            TelevisionSeason season)
        {
            foreach (string file in _localFileSystem.ListFiles(season.Path)
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f))))
            {
                // TODO: figure out how to rebuild playlists
                Either<BaseError, TelevisionEpisodeMediaItem> maybeEpisode = await _televisionRepository
                    .GetOrAddEpisode(season, localMediaSource.Id, file)
                    .BindT(episode => UpdateStatistics(episode, ffprobePath).MapT(_ => episode))
                    .BindT(UpdateMetadata)
                    .BindT(UpdateThumbnail);

                maybeEpisode.IfLeft(
                    error => _logger.LogWarning("Error processing episode at {Path}: {Error}", file, error.Value));
            }

            return Unit.Default;
        }

        private async Task<Either<BaseError, TelevisionShow>> UpdateMetadata(TelevisionShow show)
        {
            try
            {
                await LocateNfoFile(show).Match(
                    async nfoFile =>
                    {
                        if (show.Metadata == null || show.Metadata.Source == MetadataSource.Fallback ||
                            show.Metadata.LastWriteTime < _localFileSystem.GetLastWriteTime(nfoFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            await _localMetadataProvider.RefreshSidecarMetadata(show, nfoFile);
                        }
                    },
                    async () =>
                    {
                        if (show.Metadata == null)
                        {
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", show.Path);
                            await _localMetadataProvider.RefreshFallbackMetadata(show);
                        }
                    });

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionEpisodeMediaItem>> UpdateMetadata(
            TelevisionEpisodeMediaItem episode)
        {
            try
            {
                await LocateNfoFile(episode).Match(
                    async nfoFile =>
                    {
                        if (episode.Metadata == null || episode.Metadata.Source == MetadataSource.Fallback ||
                            episode.Metadata.LastWriteTime < _localFileSystem.GetLastWriteTime(nfoFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            await _localMetadataProvider.RefreshSidecarMetadata(episode, nfoFile);
                        }
                    },
                    async () =>
                    {
                        if (episode.Metadata == null)
                        {
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", episode.Path);
                            await _localMetadataProvider.RefreshFallbackMetadata(episode);
                        }
                    });

                return episode;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionShow>> UpdatePoster(TelevisionShow show)
        {
            try
            {
                await LocatePoster(show).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(show.Poster) ||
                            show.PosterLastWriteTime < _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            await SavePosterToDisk(show, posterFile, _televisionRepository.Update, 440);
                        }
                    });

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionSeason>> UpdatePoster(TelevisionSeason season)
        {
            try
            {
                await LocatePoster(season).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(season.Poster) ||
                            season.PosterLastWriteTime < _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            await SavePosterToDisk(season, posterFile, _televisionRepository.Update);
                        }
                    });

                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, TelevisionEpisodeMediaItem>> UpdateThumbnail(
            TelevisionEpisodeMediaItem episode)
        {
            try
            {
                await LocateThumbnail(episode).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(episode.Poster) ||
                            episode.PosterLastWriteTime < _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Thumbnail", posterFile);
                            await SavePosterToDisk(episode, posterFile, _televisionRepository.Update);
                        }
                    });

                return episode;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private Option<string> LocateNfoFile(TelevisionShow show) =>
            Optional(Path.Combine(show.Path, "tvshow.nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocateNfoFile(TelevisionEpisodeMediaItem episode) =>
            Optional(Path.ChangeExtension(episode.Path, "nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocatePoster(TelevisionShow show) =>
            ImageFileExtensions
                .Map(ext => $"poster.{ext}")
                .Map(f => Path.Combine(show.Path, f))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();

        private Option<string> LocatePoster(TelevisionSeason season)
        {
            string folder = Path.GetDirectoryName(season.Path) ?? string.Empty;
            return ImageFileExtensions
                .Map(ext => Path.Combine(folder, $"season{season.Number:00}-poster.{ext}"))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private Option<string> LocateThumbnail(TelevisionEpisodeMediaItem episode)
        {
            string folder = Path.GetDirectoryName(episode.Path) ?? string.Empty;
            return ImageFileExtensions
                .Map(ext => Path.GetFileNameWithoutExtension(episode.Path) + $"-thumb.{ext}")
                .Map(f => Path.Combine(folder, f))
                .Filter(f => _localFileSystem.FileExists(f))
                .HeadOrNone();
        }

        private bool ShouldIncludeFolder(string folder) =>
            !Path.GetFileName(folder).StartsWith('.') &&
            !_localFileSystem.FileExists(Path.Combine(folder, ".etvignore"));

        private static Option<int> SeasonNumberForFolder(string folder)
        {
            if (int.TryParse(folder.Split(" ").Last(), out int seasonNumber))
            {
                return seasonNumber;
            }

            if (folder.EndsWith("specials", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return None;
        }
    }
}
