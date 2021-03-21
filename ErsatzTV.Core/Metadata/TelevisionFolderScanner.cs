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
using ErsatzTV.Core.Interfaces.Search;
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
        private readonly ISearchIndex _searchIndex;
        private readonly ITelevisionRepository _televisionRepository;

        public TelevisionFolderScanner(
            ILocalFileSystem localFileSystem,
            ITelevisionRepository televisionRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            ISearchIndex searchIndex,
            ILogger<TelevisionFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            imageCache,
            logger)
        {
            _localFileSystem = localFileSystem;
            _televisionRepository = televisionRepository;
            _localMetadataProvider = localMetadataProvider;
            _searchIndex = searchIndex;
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
                Either<BaseError, MediaItemScanResult<Show>> maybeShow =
                    await FindOrCreateShow(libraryPath.Id, showFolder)
                        .BindT(show => UpdateMetadataForShow(show, showFolder))
                        .BindT(show => UpdateArtworkForShow(show, showFolder, ArtworkKind.Poster))
                        .BindT(show => UpdateArtworkForShow(show, showFolder, ArtworkKind.FanArt));

                await maybeShow.Match(
                    async result =>
                    {
                        if (result.IsAdded)
                        {
                            await _searchIndex.AddItems(new List<MediaItem> { result.Item });
                        }
                        else if (result.IsUpdated)
                        {
                            await _searchIndex.UpdateItems(new List<MediaItem> { result.Item });
                        }

                        await ScanSeasons(libraryPath, ffprobePath, result.Item, showFolder);
                    },
                    _ => Task.FromResult(Unit.Default));
            }

            foreach (string path in await _televisionRepository.FindEpisodePaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Removing missing episode at {Path}", path);
                    await _televisionRepository.DeleteByPath(libraryPath, path);
                }
            }

            await _televisionRepository.DeleteEmptySeasons(libraryPath);
            List<int> ids = await _televisionRepository.DeleteEmptyShows(libraryPath);
            await _searchIndex.RemoveItems(ids);

            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<Show>>> FindOrCreateShow(
            int libraryPathId,
            string showFolder)
        {
            ShowMetadata metadata = await _localMetadataProvider.GetMetadataForShow(showFolder);
            Option<Show> maybeShow = await _televisionRepository.GetShowByMetadata(libraryPathId, metadata);
            return await maybeShow.Match(
                show => Right<BaseError, MediaItemScanResult<Show>>(new MediaItemScanResult<Show>(show)).AsTask(),
                async () => await _televisionRepository.AddShow(libraryPathId, showFolder, metadata));
        }

        private async Task<Unit> ScanSeasons(
            LibraryPath libraryPath,
            string ffprobePath,
            Show show,
            string showFolder)
        {
            foreach (string seasonFolder in _localFileSystem.ListSubdirectories(showFolder).Filter(ShouldIncludeFolder)
                .OrderBy(identity))
            {
                Option<int> maybeSeasonNumber = SeasonNumberForFolder(seasonFolder);
                await maybeSeasonNumber.IfSomeAsync(
                    async seasonNumber =>
                    {
                        Either<BaseError, Season> maybeSeason = await _televisionRepository
                            .GetOrAddSeason(show, libraryPath.Id, seasonNumber)
                            .BindT(season => UpdatePoster(season, seasonFolder));

                        await maybeSeason.Match(
                            season => ScanEpisodes(libraryPath, ffprobePath, season, seasonFolder),
                            _ => Task.FromResult(Unit.Default));
                    });
            }

            return Unit.Default;
        }

        private async Task<Unit> ScanEpisodes(
            LibraryPath libraryPath,
            string ffprobePath,
            Season season,
            string seasonPath)
        {
            foreach (string file in _localFileSystem.ListFiles(seasonPath)
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f))).OrderBy(identity))
            {
                // TODO: figure out how to rebuild playlists
                Either<BaseError, Episode> maybeEpisode = await _televisionRepository
                    .GetOrAddEpisode(season, libraryPath, file)
                    .BindT(
                        episode => UpdateStatistics(new MediaItemScanResult<Episode>(episode), ffprobePath)
                            .MapT(_ => episode))
                    .BindT(UpdateMetadata)
                    .BindT(UpdateThumbnail);

                maybeEpisode.IfLeft(
                    error => _logger.LogWarning("Error processing episode at {Path}: {Error}", file, error.Value));
            }

            return Unit.Default;
        }

        private async Task<Either<BaseError, MediaItemScanResult<Show>>> UpdateMetadataForShow(
            MediaItemScanResult<Show> result,
            string showFolder)
        {
            try
            {
                Show show = result.Item;
                await LocateNfoFileForShow(showFolder).Match(
                    async nfoFile =>
                    {
                        bool shouldUpdate = Optional(show.ShowMetadata).Flatten().HeadOrNone().Match(
                            m => m.MetadataKind == MetadataKind.Fallback ||
                                 m.DateUpdated < _localFileSystem.GetLastWriteTime(nfoFile),
                            true);

                        if (shouldUpdate)
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Sidecar Metadata", nfoFile);
                            if (await _localMetadataProvider.RefreshSidecarMetadata(show, nfoFile))
                            {
                                result.IsUpdated = true;
                            }
                        }
                    },
                    async () =>
                    {
                        if (!Optional(show.ShowMetadata).Flatten().Any())
                        {
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", showFolder);
                            if (await _localMetadataProvider.RefreshFallbackMetadata(show, showFolder))
                            {
                                result.IsUpdated = true;
                            }
                        }
                    });

                return result;
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
                        bool shouldUpdate = Optional(episode.EpisodeMetadata).Flatten().HeadOrNone().Match(
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
                        bool shouldUpdate = Optional(episode.EpisodeMetadata).Flatten().HeadOrNone().Match(
                            m => m.DateUpdated == DateTime.MinValue,
                            true);

                        if (shouldUpdate)
                        {
                            string path = episode.MediaVersions.Head().MediaFiles.Head().Path;
                            _logger.LogDebug("Refreshing {Attribute} for {Path}", "Fallback Metadata", path);
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

        private async Task<Either<BaseError, MediaItemScanResult<Show>>> UpdateArtworkForShow(
            MediaItemScanResult<Show> result,
            string showFolder,
            ArtworkKind artworkKind)
        {
            try
            {
                Show show = result.Item;
                await LocateArtworkForShow(showFolder, artworkKind).IfSomeAsync(
                    async posterFile =>
                    {
                        ShowMetadata metadata = show.ShowMetadata.Head();
                        await RefreshArtwork(posterFile, metadata, artworkKind);
                    });

                return result;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Season>> UpdatePoster(Season season, string seasonFolder)
        {
            try
            {
                await LocatePoster(season, seasonFolder).IfSomeAsync(
                    async posterFile =>
                    {
                        SeasonMetadata metadata = season.SeasonMetadata.Head();
                        await RefreshArtwork(posterFile, metadata, ArtworkKind.Poster);
                    });

                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.Message);
            }
        }

        private async Task<Either<BaseError, Episode>> UpdateThumbnail(Episode episode)
        {
            try
            {
                await LocateThumbnail(episode).IfSomeAsync(
                    async posterFile =>
                    {
                        EpisodeMetadata metadata = episode.EpisodeMetadata.Head();
                        await RefreshArtwork(posterFile, metadata, ArtworkKind.Thumbnail);
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

        private Option<string> LocateNfoFile(Episode episode)
        {
            string path = episode.MediaVersions.Head().MediaFiles.Head().Path;
            return Optional(Path.ChangeExtension(path, "nfo"))
                .Filter(s => _localFileSystem.FileExists(s));
        }

        private Option<string> LocateArtworkForShow(string showFolder, ArtworkKind artworkKind)
        {
            string segment = artworkKind switch
            {
                ArtworkKind.Poster => "poster",
                ArtworkKind.FanArt => "fanart",
                _ => throw new ArgumentOutOfRangeException(nameof(artworkKind))
            };

            return ImageFileExtensions
                .Map(ext => $"{segment}.{ext}")
                .Map(f => Path.Combine(showFolder, f))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private Option<string> LocatePoster(Season season, string seasonFolder)
        {
            string folder = Path.GetDirectoryName(seasonFolder) ?? string.Empty;
            return ImageFileExtensions
                .Map(ext => Path.Combine(folder, $"season{season.SeasonNumber:00}-poster.{ext}"))
                .Filter(s => _localFileSystem.FileExists(s))
                .HeadOrNone();
        }

        private Option<string> LocateThumbnail(Episode episode)
        {
            string path = episode.MediaVersions.Head().MediaFiles.Head().Path;
            string folder = Path.GetDirectoryName(path) ?? string.Empty;
            return ImageFileExtensions
                .Map(ext => Path.GetFileNameWithoutExtension(path) + $"-thumb.{ext}")
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
