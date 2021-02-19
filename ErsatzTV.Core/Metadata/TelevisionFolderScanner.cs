using System;
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
                .OrderBy(identity)
                .ToList();

            foreach (string showFolder in allShowFolders)
            {
                // TODO: check all sources for latest metadata?
                Either<BaseError, TelevisionShow> maybeShow =
                    await FindOrCreateShow(localMediaSource.Id, showFolder)
                        .BindT(show => UpdateMetadataForShow(show, showFolder))
                        .BindT(show => UpdatePosterForShow(show, showFolder));

                await maybeShow.Match(
                    show => ScanSeasons(localMediaSource, ffprobePath, show, showFolder),
                    _ => Task.FromResult(Unit.Default));
            }

            await _televisionRepository.DeleteMissingSources(localMediaSource.Id, allShowFolders);
            await _televisionRepository.DeleteEmptyShows();

            return Unit.Default;
        }

        private async Task<Either<BaseError, TelevisionShow>> FindOrCreateShow(
            int localMediaSourceId,
            string showFolder)
        {
            Option<TelevisionShow> maybeShowByPath =
                await _televisionRepository.GetShowByPath(localMediaSourceId, showFolder);
            return await maybeShowByPath.Match(
                show => Right<BaseError, TelevisionShow>(show).AsTask(),
                async () =>
                {
                    TelevisionShowMetadata metadata = await _localMetadataProvider.GetMetadataForShow(showFolder);
                    Option<TelevisionShow> maybeShow = await _televisionRepository.GetShowByMetadata(metadata);
                    return await maybeShow.Match(
                        async show =>
                        {
                            show.Sources.Add(
                                new LocalTelevisionShowSource
                                {
                                    MediaSourceId = localMediaSourceId,
                                    Path = showFolder,
                                    TelevisionShow = show
                                });
                            await _televisionRepository.Update(show);
                            return Right<BaseError, TelevisionShow>(show);
                        },
                        async () => await _televisionRepository.AddShow(localMediaSourceId, showFolder, metadata));
                });
        }

        private async Task<Unit> ScanSeasons(
            LocalMediaSource localMediaSource,
            string ffprobePath,
            TelevisionShow show,
            string showPath)
        {
            foreach (string seasonFolder in _localFileSystem.ListSubdirectories(showPath).Filter(ShouldIncludeFolder)
                .OrderBy(identity))
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
                .Filter(f => VideoFileExtensions.Contains(Path.GetExtension(f))).OrderBy(identity))
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

        private async Task<Either<BaseError, TelevisionShow>> UpdateMetadataForShow(
            TelevisionShow show,
            string showFolder)
        {
            try
            {
                await LocateNfoFileForShow(showFolder).Match(
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

        private async Task<Either<BaseError, TelevisionShow>> UpdatePosterForShow(
            TelevisionShow show,
            string showFolder)
        {
            try
            {
                await LocatePosterForShow(showFolder).IfSomeAsync(
                    async posterFile =>
                    {
                        if (string.IsNullOrWhiteSpace(show.Poster) ||
                            show.PosterLastWriteTime < _localFileSystem.GetLastWriteTime(posterFile))
                        {
                            _logger.LogDebug("Refreshing {Attribute} from {Path}", "Poster", posterFile);
                            Either<BaseError, string> maybePoster = await SavePosterToDisk(posterFile, 440);
                            await maybePoster.Match(
                                poster =>
                                {
                                    show.Poster = poster;
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

        private Option<string> LocateNfoFileForShow(string showFolder) =>
            Optional(Path.Combine(showFolder, "tvshow.nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocateNfoFile(TelevisionEpisodeMediaItem episode) =>
            Optional(Path.ChangeExtension(episode.Path, "nfo"))
                .Filter(s => _localFileSystem.FileExists(s));

        private Option<string> LocatePosterForShow(string showFolder) =>
            ImageFileExtensions
                .Map(ext => $"poster.{ext}")
                .Map(f => Path.Combine(showFolder, f))
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
