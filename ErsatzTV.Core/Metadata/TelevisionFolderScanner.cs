using System;
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

        public async Task<Either<BaseError, Unit>> ScanFolder(LibraryPath libraryPath, string ffprobePath)
        {
            if (!_localFileSystem.IsLibraryPathAccessible(libraryPath))
            {
                return new MediaSourceInaccessible();
            }

            var allShowFolders = _localFileSystem.ListSubdirectories(libraryPath.Path)
                .Filter(ShouldIncludeFolder)
                .OrderBy(identity)
                .ToList();

            foreach (string showFolder in allShowFolders)
            {
                // TODO: check all sources for latest metadata?
                Either<BaseError, Show> maybeShow =
                    await FindOrCreateShow(libraryPath.Id, showFolder)
                        .BindT(show => UpdateMetadataForShow(show, showFolder))
                        .BindT(show => UpdatePosterForShow(show, showFolder));

                await maybeShow.Match(
                    show => ScanSeasons(libraryPath, ffprobePath, show, showFolder),
                    _ => Task.FromResult(Unit.Default));
            }

            // TODO: remove this?
            // await _televisionRepository.DeleteMissingSources(libraryPath.Id, allShowFolders);
            await _televisionRepository.DeleteEmptyShows();

            return Unit.Default;
        }

        private async Task<Either<BaseError, Show>> FindOrCreateShow(
            int libraryPathId,
            string showFolder)
        {
            ShowMetadata metadata = await _localMetadataProvider.GetMetadataForShow(showFolder);
            Option<Show> maybeShow = await _televisionRepository.GetShowByMetadata(metadata);
            return await maybeShow.Match(
                show => Right<BaseError, Show>(show).AsTask(),
                async () => await _televisionRepository.AddShow(libraryPathId, showFolder, metadata));
        }

        private async Task<Unit> ScanSeasons(
            LibraryPath libraryPath,
            string ffprobePath,
            Show show,
            string showPath)
        {
            foreach (string seasonFolder in _localFileSystem.ListSubdirectories(showPath).Filter(ShouldIncludeFolder)
                .OrderBy(identity))
            {
                Option<int> maybeSeasonNumber = SeasonNumberForFolder(seasonFolder);
                await maybeSeasonNumber.IfSomeAsync(
                    async seasonNumber =>
                    {
                        Either<BaseError, Season> maybeSeason = await _televisionRepository
                            .GetOrAddSeason(show, seasonFolder, seasonNumber)
                            .BindT(UpdatePoster);

                        await maybeSeason.Match(
                            season => ScanEpisodes(libraryPath, ffprobePath, season),
                            _ => Task.FromResult(Unit.Default));
                    });
            }

            return Unit.Default;
        }

        private async Task<Unit> ScanEpisodes(
            LibraryPath libraryPath,
            string ffprobePath,
            Season season)
        {
            foreach (string file in _localFileSystem.ListFiles(season.Path)
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f))).OrderBy(identity))
            {
                // TODO: figure out how to rebuild playlists
                Either<BaseError, Episode> maybeEpisode = await _televisionRepository
                    .GetOrAddEpisode(season, libraryPath, file)
                    .BindT(episode => UpdateStatistics(episode, ffprobePath).MapT(_ => episode))
                    .BindT(UpdateMetadata)
                    .BindT(UpdateThumbnail);

                maybeEpisode.IfLeft(
                    error => _logger.LogWarning("Error processing episode at {Path}: {Error}", file, error.Value));
            }

            return Unit.Default;
        }

        private async Task<Either<BaseError, Show>> UpdateMetadataForShow(
            Show show,
            string showFolder)
        {
            try
            {
                await LocateNfoFileForShow(showFolder).Match(
                    async nfoFile =>
                    {
                        bool shouldUpdate = show.ShowMetadata.HeadOrNone().Match(
                            m => m.MetadataKind == MetadataKind.Fallback ||
                                 m.DateUpdated < _localFileSystem.GetLastWriteTime(nfoFile),
                            true);

                        if (shouldUpdate)
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            await _localMetadataProvider.RefreshSidecarMetadata(show, nfoFile);
                        }
                    },
                    async () =>
                    {
                        if (!show.ShowMetadata.Any())
                        {
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", showFolder);
                            await _localMetadataProvider.RefreshFallbackMetadata(show, showFolder);
                        }
                    });

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Episode>> UpdateMetadata(
            Episode episode)
        {
            try
            {
                await LocateNfoFile(episode).Match(
                    async nfoFile =>
                    {
                        bool shouldUpdate = episode.EpisodeMetadata.HeadOrNone().Match(
                            m => m.MetadataKind == MetadataKind.Fallback ||
                                 m.DateUpdated < _localFileSystem.GetLastWriteTime(nfoFile),
                            true);

                        if (shouldUpdate)
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            await _localMetadataProvider.RefreshSidecarMetadata(episode, nfoFile);
                        }
                    },
                    async () =>
                    {
                        if (!episode.EpisodeMetadata.Any())
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

        private async Task<Either<BaseError, Show>> UpdatePosterForShow(
            Show show,
            string showFolder)
        {
            try
            {
                await LocatePosterForShow(showFolder).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(show.Poster) ||
                            (show.PosterLastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            Either<BaseError, string> maybePoster = await SavePosterToDisk(posterFile, 440);
                            await maybePoster.Match(
                                poster =>
                                {
                                    show.Poster = poster;
                                    show.PosterLastWriteTime = _localFileSystem.GetLastWriteTime(posterFile);
                                    return _televisionRepository.Update(show);
                                },
                                error =>
                                {
                                    _logger.LogWarning(
                                        "Unable to save poster to disk from {Path}: {Error}",
                                        posterFile,
                                        error.Value);
                                    return Task.CompletedTask;
                                });
                        }
                    });

                return show;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Season>> UpdatePoster(Season season)
        {
            try
            {
                await LocatePoster(season).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(season.Poster) ||
                            (season.PosterLastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            await SavePosterToDisk(season, posterFile, _televisionRepository.Update, 440);
                        }
                    });

                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Episode>> UpdateThumbnail(
            Episode episode)
        {
            try
            {
                await LocateThumbnail(episode).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(episode.Poster) ||
                            (episode.PosterLastWriteTime ?? DateTime.MinValue) <
                            _localFileSystem.GetLastWriteTime(posterFile))
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

        private Option<string> LocateNfoFileForShow(string showFolder) =>
            Optional(Path.Combine(showFolder, "tvshow.nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocateNfoFile(Episode episode) =>
            Optional(Path.ChangeExtension(episode.Path, "nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocatePosterForShow(string showFolder) =>
            ImageFileExtensions
                .Map(ext => $"poster.{ext}")
                .Map(f => Path.Combine(showFolder, f))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();

        private Option<string> LocatePoster(Season season)
        {
            string folder = Path.GetDirectoryName(season.Path) ?? string.Empty;
            return ImageFileExtensions
                .Map(ext => Path.Combine(folder, $"season{season.SeasonNumber:00}-poster.{ext}"))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private Option<string> LocateThumbnail(Episode episode)
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
