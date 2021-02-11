using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalMediaScanner : ILocalMediaScanner
    {
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
            ILogger<LocalMediaScanner> logger)
        {
            _mediaItemRepository = mediaItemRepository;
            _playoutRepository = playoutRepository;
            _localStatisticsProvider = localStatisticsProvider;
            _localMetadataProvider = localMetadataProvider;
            _smartCollectionBuilder = smartCollectionBuilder;
            _playoutBuilder = playoutBuilder;
            _logger = logger;
        }

        public async Task<Unit> ScanLocalMediaSource(LocalMediaSource localMediaSource, string ffprobePath)
        {
            if (!Directory.Exists(localMediaSource.Folder))
            {
                _logger.LogWarning(
                    "Media source folder {Folder} does not exist; skipping scan",
                    localMediaSource.Folder);
                return Unit.Default;
            }

            List<MediaItem> knownMediaItems = await _mediaItemRepository.GetAllByMediaSourceId(localMediaSource.Id);
            var modifiedPlayoutIds = new List<int>();

            // remove files that no longer exist
            // add new files
            // refresh metadata for any files where it is missing
            var knownExtensions = new List<string>
            {
                ".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".ogg", ".mp4", ".m4p", ".m4v",
                ".avi", ".wmv", ".mov", ".mkv", ".ts"
            };

            var allFiles = Directory.GetFiles(localMediaSource.Folder, "*", SearchOption.AllDirectories)
                .Filter(file => knownExtensions.Contains(Path.GetExtension(file)))
                .ToSeq();

            // check if the media item exists
            (Seq<string> newFiles, Seq<MediaItem> existingMediaItems) = allFiles.Map(
                    s => Optional(knownMediaItems.Find(i => i.Path == s)).ToEither(s))
                .Partition();

            // TODO: flag as missing? delete after some period of time?
            var removedMediaItems = knownMediaItems.Filter(i => !allFiles.Contains(i.Path)).ToSeq();
            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(removedMediaItems));
            foreach (MediaItem mediaItem in removedMediaItems)
            {
                _logger.LogDebug("Removing missing local media item {MediaItem}", mediaItem.Path);
                await _mediaItemRepository.Delete(mediaItem.Id);
            }

            // if exists, check if the file was modified
            // also, try to re-categorize "other" by refreshing metadata
            Seq<MediaItem> modifiedMediaItems = existingMediaItems.Filter(
                mediaItem =>
                {
                    DateTime lastWrite = File.GetLastWriteTimeUtc(mediaItem.Path);
                    bool modified = lastWrite > mediaItem.LastWriteTime.IfNone(DateTime.MinValue);
                    return modified || mediaItem.Metadata == null || mediaItem.Metadata.MediaType == MediaType.Other;
                });
            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(modifiedMediaItems));
            foreach (MediaItem mediaItem in modifiedMediaItems)
            {
                _logger.LogDebug("Refreshing metadata for media item {MediaItem}", mediaItem.Path);
                await RefreshMetadata(mediaItem, ffprobePath);
            }

            // if new, add and store mtime, refresh metadata
            var addedMediaItems = new Seq<MediaItem>();
            foreach (string path in newFiles)
            {
                _logger.LogDebug("Adding new media item {MediaItem}", path);
                var mediaItem = new MediaItem
                {
                    MediaSourceId = localMediaSource.Id,
                    Path = path,
                    LastWriteTime = File.GetLastWriteTimeUtc(path)
                };

                await _mediaItemRepository.Add(mediaItem);
                await RefreshMetadata(mediaItem, ffprobePath);
                addedMediaItems.Add(mediaItem);
            }

            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(addedMediaItems));

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

            return Unit.Default;
        }

        private async Task RefreshMetadata(MediaItem mediaItem, string ffprobePath)
        {
            await _localStatisticsProvider.RefreshStatistics(ffprobePath, mediaItem);
            await _localMetadataProvider.RefreshMetadata(mediaItem);
            await _smartCollectionBuilder.RefreshSmartCollections(mediaItem);
        }
    }
}
