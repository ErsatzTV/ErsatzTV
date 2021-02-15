using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;
using Seq = LanguageExt.Seq;

namespace ErsatzTV.Core.Metadata
{
    public class LocalMediaScanner : ILocalMediaScanner
    {
        private readonly IImageCache _imageCache;
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMediaSourcePlanner _localMediaSourcePlanner;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILocalStatisticsProvider _localStatisticsProvider;
        private readonly ILogger<LocalMediaScanner> _logger;
        private readonly IMediaItemRepository _mediaItemRepository;
        private readonly IPlayoutBuilder _playoutBuilder;
        private readonly IPlayoutRepository _playoutRepository;
        private readonly ISmartCollectionBuilder _smartCollectionBuilder;

        public LocalMediaScanner(
            IMediaItemRepository mediaItemRepository,
            IPlayoutRepository playoutRepository,
            ILocalStatisticsProvider localStatisticsProvider,
            ILocalMetadataProvider localMetadataProvider,
            ISmartCollectionBuilder smartCollectionBuilder,
            IPlayoutBuilder playoutBuilder,
            ILocalMediaSourcePlanner localMediaSourcePlanner,
            ILocalFileSystem localFileSystem,
            IImageCache imageCache,
            ILogger<LocalMediaScanner> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _playoutRepository = playoutRepository;
            _localStatisticsProvider = localStatisticsProvider;
            _localMetadataProvider = localMetadataProvider;
            _smartCollectionBuilder = smartCollectionBuilder;
            _playoutBuilder = playoutBuilder;
            _localMediaSourcePlanner = localMediaSourcePlanner;
            _localFileSystem = localFileSystem;
            _imageCache = imageCache;
            _logger = logger;
        }

        public async Task<Unit> ScanLocalMediaSource(
            LocalMediaSource localMediaSource,
            string ffprobePath,
            ScanningMode scanningMode)
        {
            if (!_localFileSystem.IsMediaSourceAccessible(localMediaSource))
            {
                _logger.LogWarning(
                    "Media source folder {Folder} does not exist or is inaccessible; skipping scan",
                    localMediaSource.Folder);
                return unit;
            }

            List<MediaItem> knownMediaItems = await _mediaItemRepository.GetAllByMediaSourceId(localMediaSource.Id);
            var modifiedPlayoutIds = new List<int>();

            Seq<LocalMediaSourcePlan> actions = _localMediaSourcePlanner.DetermineActions(
                localMediaSource.MediaType,
                knownMediaItems.ToSeq(),
                FindAllFiles(localMediaSource));

            foreach (LocalMediaSourcePlan action in actions)
            {
                Option<ActionPlan> maybeAddPlan =
                    action.ActionPlans.SingleOrDefault(plan => plan.TargetAction == ScanningAction.Add);
                await maybeAddPlan.IfSomeAsync(
                    async plan =>
                    {
                        Option<MediaItem> maybeMediaItem = await AddMediaItem(localMediaSource, plan.TargetPath);

                        // any actions other than "add" need to operate on a media item
                        maybeMediaItem.IfSome(mediaItem => action.Source = mediaItem);
                    });

                foreach (ActionPlan plan in action.ActionPlans.OrderBy(plan => (int) plan.TargetAction))
                {
                    string sourcePath = action.Source.Match(
                        mediaItem => mediaItem.Path,
                        path => path);

                    _logger.LogDebug(
                        "{Source}: {Action} with {File}",
                        Path.GetFileName(sourcePath),
                        plan.TargetAction,
                        Path.GetRelativePath(Path.GetDirectoryName(sourcePath) ?? string.Empty, plan.TargetPath));

                    await action.Source.Match(
                        async mediaItem =>
                        {
                            var changed = false;

                            switch (plan.TargetAction)
                            {
                                case ScanningAction.Remove:
                                    await RemoveMissingItem(mediaItem);
                                    break;
                                case ScanningAction.Poster:
                                    await SavePosterForItem(mediaItem, plan.TargetPath);
                                    break;
                                case ScanningAction.FallbackMetadata:
                                    await RefreshFallbackMetadataForItem(mediaItem);
                                    break;
                                case ScanningAction.SidecarMetadata:
                                    await RefreshSidecarMetadataForItem(mediaItem, plan.TargetPath);
                                    break;
                                case ScanningAction.Statistics:
                                    changed = await RefreshStatisticsForItem(mediaItem, ffprobePath);
                                    break;
                                case ScanningAction.Collections:
                                    changed = await RefreshCollectionsForItem(mediaItem);
                                    break;
                            }

                            if (changed)
                            {
                                List<int> ids =
                                    await _playoutRepository.GetPlayoutIdsForMediaItems(Seq.create(mediaItem));
                                modifiedPlayoutIds.AddRange(ids);
                            }
                        },
                        path =>
                        {
                            _logger.LogError("This is a bug, something went wrong processing {Path}", path);
                            return Task.CompletedTask;
                        });
                }
            }

            foreach (int playoutId in modifiedPlayoutIds.Distinct())
            {
                Option<Playout> maybePlayout = await _playoutRepository.GetFull(playoutId);
                await maybePlayout.Match(
                    async playout =>
                    {
                        Playout result = await _playoutBuilder.BuildPlayoutItems(playout, true);
                        await _playoutRepository.Update(result);
                    },
                    Task.CompletedTask);
            }

            return unit;
        }

        private Seq<string> FindAllFiles(LocalMediaSource localMediaSource)
        {
            Seq<string> allDirectories = Directory
                .GetDirectories(localMediaSource.Folder, "*", SearchOption.AllDirectories)
                .ToSeq()
                .Add(localMediaSource.Folder);

            // remove any directories with an .etvignore file locally, or in any parent directory
            Seq<string> excluded = allDirectories.Filter(path => File.Exists(Path.Combine(path, ".etvignore")));
            Seq<string> relevantDirectories = allDirectories
                .Filter(d => !excluded.Any(d.StartsWith));
            // .Filter(d => localMediaSource.MediaType == MediaType.Other || !IsExtrasFolder(d));

            return relevantDirectories
                .Collect(d => Directory.GetFiles(d, "*", SearchOption.TopDirectoryOnly))
                .OrderBy(identity)
                .ToSeq();
        }

        private async Task<Option<MediaItem>> AddMediaItem(MediaSource mediaSource, string path)
        {
            try
            {
                var mediaItem = new MediaItem
                {
                    MediaSourceId = mediaSource.Id,
                    Path = path,
                    LastWriteTime = File.GetLastWriteTimeUtc(path)
                };

                await _mediaItemRepository.Add(mediaItem);

                return mediaItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add media item for {Path}", path);
                return None;
            }
        }

        private async Task RemoveMissingItem(MediaItem mediaItem)
        {
            try
            {
                await _mediaItemRepository.Delete(mediaItem.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove missing local media item {MediaItem}", mediaItem.Path);
            }
        }

        private async Task SavePosterForItem(MediaItem mediaItem, string posterPath)
        {
            try
            {
                byte[] originalBytes = await File.ReadAllBytesAsync(posterPath);
                Either<BaseError, string> maybeHash = await _imageCache.ResizeAndSaveImage(originalBytes, 220, null);
                await maybeHash.Match(
                    hash =>
                    {
                        mediaItem.Poster = hash;
                        mediaItem.PosterLastWriteTime = File.GetLastWriteTimeUtc(posterPath);
                        return _mediaItemRepository.Update(mediaItem);
                    },
                    error =>
                    {
                        _logger.LogWarning(
                            "Unable to save poster to disk from {Path}: {Error}",
                            posterPath,
                            error.Value);
                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh poster for media item {MediaItem}", mediaItem.Path);
            }
        }

        private async Task<bool> RefreshStatisticsForItem(MediaItem mediaItem, string ffprobePath)
        {
            try
            {
                return await _localStatisticsProvider.RefreshStatistics(ffprobePath, mediaItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh statistics for media item {MediaItem}", mediaItem.Path);
                return false;
            }
        }

        private async Task<bool> RefreshCollectionsForItem(MediaItem mediaItem)
        {
            try
            {
                return await _smartCollectionBuilder.RefreshSmartCollections(mediaItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh collections for media item {MediaItem}", mediaItem.Path);
                return false;
            }
        }

        private async Task RefreshSidecarMetadataForItem(MediaItem mediaItem, string path)
        {
            try
            {
                await _localMetadataProvider.RefreshSidecarMetadata(mediaItem, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh nfo metadata for media item {MediaItem}", mediaItem.Path);
            }
        }

        private async Task RefreshFallbackMetadataForItem(MediaItem mediaItem)
        {
            try
            {
                await _localMetadataProvider.RefreshFallbackMetadata(mediaItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh fallback metadata for media item {MediaItem}", mediaItem.Path);
            }
        }
    }
}
