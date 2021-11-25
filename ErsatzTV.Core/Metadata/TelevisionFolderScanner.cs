using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Errors;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Search;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Core.Metadata
{
    public class TelevisionFolderScanner : LocalFolderScanner, ITelevisionFolderScanner
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILogger<TelevisionFolderScanner> _logger;
        private readonly IMediator _mediator;
        private readonly IMetadataRepository _metadataRepository;
        private readonly ISearchIndex _searchIndex;
        private readonly ISearchRepository _searchRepository;
        private readonly ITelevisionRepository _televisionRepository;

        public TelevisionFolderScanner(
            ILocalFileSystem localFileSystem,
            ITelevisionRepository televisionRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            IMetadataRepository metadataRepository,
            IImageCache imageCache,
            ISearchIndex searchIndex,
            ISearchRepository searchRepository,
            ILibraryRepository libraryRepository,
            IMediator mediator,
            FFmpegProcessService ffmpegProcessService,
            ILogger<TelevisionFolderScanner> logger) : base(
            localFileSystem,
            localStatisticsProvider,
            metadataRepository,
            imageCache,
            ffmpegProcessService,
            logger)
        {
            _localFileSystem = localFileSystem;
            _televisionRepository = televisionRepository;
            _localMetadataProvider = localMetadataProvider;
            _metadataRepository = metadataRepository;
            _searchIndex = searchIndex;
            _searchRepository = searchRepository;
            _libraryRepository = libraryRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Either<BaseError, Unit>> ScanFolder(
            LibraryPath libraryPath,
            string ffprobePath,
            decimal progressMin,
            decimal progressMax)
        {
            decimal progressSpread = progressMax - progressMin;

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
                decimal percentCompletion = (decimal) allShowFolders.IndexOf(showFolder) / allShowFolders.Count;
                await _mediator.Publish(
                    new LibraryScanProgress(libraryPath.LibraryId, progressMin + percentCompletion * progressSpread));

                Either<BaseError, MediaItemScanResult<Show>> maybeShow =
                    await FindOrCreateShow(libraryPath.Id, showFolder)
                        .BindT(show => UpdateMetadataForShow(show, showFolder))
                        .BindT(show => UpdateArtworkForShow(show, showFolder, ArtworkKind.Poster))
                        .BindT(show => UpdateArtworkForShow(show, showFolder, ArtworkKind.FanArt))
                        .BindT(show => UpdateArtworkForShow(show, showFolder, ArtworkKind.Thumbnail));

                await maybeShow.Match(
                    async result =>
                    {
                        await ScanSeasons(
                            libraryPath,
                            ffprobePath,
                            result.Item,
                            showFolder);

                        if (result.IsAdded)
                        {
                            await _searchIndex.AddItems(_searchRepository, new List<MediaItem> { result.Item });
                        }
                        else if (result.IsUpdated)
                        {
                            await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { result.Item });
                        }
                    },
                    error =>
                    {
                        _logger.LogWarning(
                            "Error processing show in folder {Folder}: {Error}",
                            showFolder,
                            error.Value);
                        return Task.FromResult(Unit.Default);
                    });
            }

            foreach (string path in await _televisionRepository.FindEpisodePaths(libraryPath))
            {
                if (!_localFileSystem.FileExists(path))
                {
                    _logger.LogInformation("Removing missing episode at {Path}", path);
                    await _televisionRepository.DeleteByPath(libraryPath, path);
                }
                else if (Path.GetFileName(path).StartsWith("._"))
                {
                    _logger.LogInformation("Removing dot underscore file at {Path}", path);
                    await _televisionRepository.DeleteByPath(libraryPath, path);
                }
            }

            await _televisionRepository.DeleteEmptySeasons(libraryPath);
            List<int> ids = await _televisionRepository.DeleteEmptyShows(libraryPath);
            await _searchIndex.RemoveItems(ids);

            _searchIndex.Commit();
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
                string etag = FolderEtag.CalculateWithSubfolders(seasonFolder, _localFileSystem);
                Option<LibraryFolder> knownFolder = libraryPath.LibraryFolders
                    .Filter(f => f.Path == seasonFolder)
                    .HeadOrNone();

                // skip folder if etag matches
                if (await knownFolder.Map(f => f.Etag ?? string.Empty).IfNoneAsync(string.Empty) == etag)
                {
                    continue;
                }

                Option<int> maybeSeasonNumber = SeasonNumberForFolder(seasonFolder);
                await maybeSeasonNumber.IfSomeAsync(
                    async seasonNumber =>
                    {
                        Either<BaseError, Season> maybeSeason = await _televisionRepository
                            .GetOrAddSeason(show, libraryPath.Id, seasonNumber)
                            .BindT(EnsureMetadataExists)
                            .BindT(season => UpdatePoster(season, seasonFolder));

                        await maybeSeason.Match(
                            async season =>
                            {
                                await ScanEpisodes(libraryPath, ffprobePath, season, seasonFolder);
                                await _libraryRepository.SetEtag(libraryPath, knownFolder, seasonFolder, etag);

                                season.Show = show;
                                await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { season });
                            },
                            error =>
                            {
                                _logger.LogWarning(
                                    "Error processing season in folder {Folder}: {Error}",
                                    seasonFolder,
                                    error.Value);
                                return Task.FromResult(Unit.Default);
                            });
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
            var allSeasonFiles = _localFileSystem.ListSubdirectories(seasonPath)
                .Map(_localFileSystem.ListFiles)
                .Flatten()
                .Append(_localFileSystem.ListFiles(seasonPath))
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f)))
                .Filter(f => !Path.GetFileName(f).StartsWith("._"))
                .OrderBy(identity)
                .ToList();

            foreach (string file in allSeasonFiles)
            {
                // TODO: figure out how to rebuild playlists
                Either<BaseError, Episode> maybeEpisode = await _televisionRepository
                    .GetOrAddEpisode(season, libraryPath, file)
                    .BindT(
                        episode => UpdateStatistics(new MediaItemScanResult<Episode>(episode), ffprobePath)
                            .MapT(_ => episode))
                    .BindT(UpdateMetadata)
                    .BindT(UpdateThumbnail);

                await maybeEpisode.Match(
                    async episode =>
                    {
                        await _searchIndex.UpdateItems(_searchRepository, new List<MediaItem> { episode });
                    },
                    error =>
                    {
                        _logger.LogWarning("Error processing episode at {Path}: {Error}", file, error.Value);
                        return Task.CompletedTask;
                    });
            }

            // TODO: remove missing episodes?

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
                                 m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
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
                return BaseError.New(ex.ToString());
            }
        }

        private async Task<Either<BaseError, Season>> EnsureMetadataExists(Season season)
        {
            season.SeasonMetadata ??= new List<SeasonMetadata>();

            if (!season.SeasonMetadata.Any())
            {
                var metadata = new SeasonMetadata
                {
                    SeasonId = season.Id,
                    Season = season,
                    DateAdded = DateTime.UtcNow,
                    Guids = new List<MetadataGuid>()
                };

                season.SeasonMetadata.Add(metadata);
                await _metadataRepository.Add(metadata);
            }

            return season;
        }

        private async Task<Either<BaseError, Episode>> UpdateMetadata(Episode episode)
        {
            try
            {
                await LocateNfoFile(episode).Match(
                    async nfoFile =>
                    {
                        bool shouldUpdate = Optional(episode.EpisodeMetadata).Flatten().HeadOrNone().Match(
                            m => m.MetadataKind == MetadataKind.Fallback ||
                                 m.DateUpdated != _localFileSystem.GetLastWriteTime(nfoFile),
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
                            m => m.DateUpdated == SystemTime.MinValueUtc,
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
                return BaseError.New(ex.ToString());
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
                    async artworkFile =>
                    {
                        ShowMetadata metadata = show.ShowMetadata.Head();
                        await RefreshArtwork(artworkFile, metadata, artworkKind, None);
                    });

                return result;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
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
                        await RefreshArtwork(posterFile, metadata, ArtworkKind.Poster, None);
                    });

                return season;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
            }
        }

        private async Task<Either<BaseError, Episode>> UpdateThumbnail(Episode episode)
        {
            try
            {
                await LocateThumbnail(episode).IfSomeAsync(
                    async posterFile =>
                    {
                        foreach (EpisodeMetadata metadata in episode.EpisodeMetadata)
                        {
                            await RefreshArtwork(posterFile, metadata, ArtworkKind.Thumbnail, None);
                        }
                    });

                return episode;
            }
            catch (Exception ex)
            {
                return BaseError.New(ex.ToString());
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
            string[] segments = artworkKind switch
            {
                ArtworkKind.Poster => new[] { "poster", "folder" },
                ArtworkKind.FanArt => new[] { "fanart" },
                ArtworkKind.Thumbnail => new[] { "thumb" },
                _ => throw new ArgumentOutOfRangeException(nameof(artworkKind))
            };

            return ImageFileExtensions
                .Map(ext => segments.Map(segment => $"{segment}.{ext}"))
                .Flatten()
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
