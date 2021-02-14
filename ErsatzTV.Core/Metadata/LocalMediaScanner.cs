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
        private readonly ILocalFileSystem _localFileSystem;
        private readonly ILocalMetadataProvider _localMetadataProvider;
        private readonly ILocalPosterProvider _localPosterProvider;
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
            ILocalPosterProvider localPosterProvider,
            ISmartCollectionBuilder smartCollectionBuilder,
            IPlayoutBuilder playoutBuilder,
            ILogger<LocalMediaScanner> logger,
            ILocalFileSystem localFileSystem)
        {
            _mediaItemRepository = mediaItemRepository;
            _playoutRepository = playoutRepository;
            _localStatisticsProvider = localStatisticsProvider;
            _localMetadataProvider = localMetadataProvider;
            _localPosterProvider = localPosterProvider;
            _smartCollectionBuilder = smartCollectionBuilder;
            _playoutBuilder = playoutBuilder;
            _logger = logger;
            _localFileSystem = localFileSystem;
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

            Seq<string> allFiles = _localFileSystem.FindRelevantVideos(localMediaSource);

            // check if the media item exists
            (Seq<string> newFiles, Seq<MediaItem> existingMediaItems) = allFiles.Map(
                    s => Optional(knownMediaItems.Find(i => i.Path == s)).ToEither(s))
                .Partition();

            // remove media items that no longer exist
            var missingMediaItems = knownMediaItems.Filter(i => !allFiles.Contains(i.Path)).ToSeq();
            await RemoveMissingItems(missingMediaItems);
            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(missingMediaItems));

            Seq<MediaItem> staleMetadataMediaItems = scanningMode == ScanningMode.RescanAll
                ? existingMediaItems
                : existingMediaItems.Filter(i => _localFileSystem.ShouldRefreshMetadata(localMediaSource, i));
            Seq<MediaItem> modifiedMediaItems = await RefreshMetadataForItems(ffprobePath, staleMetadataMediaItems);
            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(modifiedMediaItems));

            // if new, add and store mtime, refresh metadata
            var addedMediaItems = new List<MediaItem>();
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

            modifiedPlayoutIds.AddRange(await _playoutRepository.GetPlayoutIdsForMediaItems(addedMediaItems.ToSeq()));

            Seq<MediaItem> stalePosterMediaItems = existingMediaItems
                .Filter(_localFileSystem.ShouldRefreshPoster)
                .Concat(addedMediaItems);
            await RefreshPosterForItems(stalePosterMediaItems);

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

        private async Task<Seq<MediaItem>> RefreshMetadataForItems(
            string ffprobePath,
            Seq<MediaItem> staleMetadataMediaItems)
        {
            var modifiedMediaItems = new List<MediaItem>();
            foreach (MediaItem mediaItem in staleMetadataMediaItems)
            {
                _logger.LogDebug("Refreshing metadata for media item {MediaItem}", mediaItem.Path);
                if (await RefreshMetadata(mediaItem, ffprobePath))
                {
                    // only queue playout rebuilds for media items
                    // where the duration or collections have changed
                    modifiedMediaItems.Add(mediaItem);
                }
            }

            return modifiedMediaItems.ToSeq();
        }

        private async Task RefreshPosterForItems(Seq<MediaItem> stalePosterMediaItems)
        {
            (Seq<MediaItem> movies, Seq<MediaItem> episodes) = stalePosterMediaItems
                .Map(i => Optional(i).Filter(i2 => i2.Metadata?.MediaType == MediaType.TvShow).ToEither(i))
                .Partition();

            // there's a 1:1 movie:poster, so refresh all
            foreach (MediaItem movie in movies)
            {
                _logger.LogDebug("Refreshing poster for media item {MediaItem}", movie.Path);
                await _localPosterProvider.RefreshPoster(movie);
            }

            // we currently have 1 poster per series, so pick the first from each group
            IEnumerable<MediaItem> episodesToRefresh = episodes.GroupBy(e => e.Metadata.Title)
                .SelectMany(g => (Option<MediaItem>) g.FirstOrDefault());

            foreach (MediaItem episode in episodesToRefresh)
            {
                _logger.LogDebug("Refreshing poster for media item {MediaItem}", episode.Path);
                await _localPosterProvider.RefreshPoster(episode);
            }
        }

        private async Task RemoveMissingItems(Seq<MediaItem> removedMediaItems)
        {
            // TODO: flag as missing? delete after some period of time?
            foreach (MediaItem mediaItem in removedMediaItems)
            {
                _logger.LogDebug("Removing missing local media item {MediaItem}", mediaItem.Path);
                await _mediaItemRepository.Delete(mediaItem.Id);
            }
        }

        private async Task<bool> RefreshMetadata(MediaItem mediaItem, string ffprobePath)
        {
            bool durationChange = await _localStatisticsProvider.RefreshStatistics(ffprobePath, mediaItem);
            await _localMetadataProvider.RefreshMetadata(mediaItem);
            bool collectionChange = await _smartCollectionBuilder.RefreshSmartCollections(mediaItem);
            return durationChange || collectionChange;
        }
    }
}
