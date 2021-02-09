using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using ErsatzTV.Api.Sdk.Api;
using ErsatzTV.Api.Sdk.Model;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.CommandLine.Commands.MediaCollections
{
    [Command("collection add-items", Description = "Ensure media collection exists and contains requested media items")]
    public class MediaCollectionMediaItemsCommand : MediaItemCommandBase
    {
        private readonly ILogger<MediaCollectionMediaItemsCommand> _logger;
        private readonly string _serverUrl;

        public MediaCollectionMediaItemsCommand(
            IConfiguration configuration,
            ILogger<MediaCollectionMediaItemsCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        [CommandParameter(0, Name = "collection-name", Description = "The name of the media collection")]
        public string Name { get; set; }

        public override async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                Either<Error, List<string>> maybeFileNames = await GetFileNames();
                await maybeFileNames.Match(
                    allFiles => SynchronizeMediaItemsToCollection(cancellationToken, allFiles),
                    error =>
                    {
                        _logger.LogError("{Error}", error.Message);
                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to synchronize media items to media collection: {Error}", ex.Message);
            }
        }

        private async Task SynchronizeMediaItemsToCollection(CancellationToken cancellationToken, List<string> allFiles)
        {
            Either<Error, Unit> result = await GetMediaSourceIdAsync(cancellationToken)
                .BindAsync(mediaSourceId => SynchronizeMediaItemsAsync(mediaSourceId, allFiles, cancellationToken))
                .BindAsync(mediaItemIds => SynchronizeMediaItemsToCollectionAsync(mediaItemIds, cancellationToken));

            result.Match(
                _ => _logger.LogInformation(
                    "Successfully synchronized {Count} media items to media collection {MediaCollection}",
                    allFiles.Count,
                    Name),
                error => _logger.LogError(
                    "Unable to synchronize media items to media collection: {Error}",
                    error.Message));
        }

        private async Task<Either<Error, int>> GetMediaSourceIdAsync(CancellationToken cancellationToken)
        {
            var mediaSourcesApi = new MediaSourcesApi(_serverUrl);
            List<MediaSourceViewModel> allMediaSources =
                await mediaSourcesApi.ApiMediaSourcesGetAsync(cancellationToken);
            Option<MediaSourceViewModel> maybeLocalMediaSource =
                allMediaSources.SingleOrDefault(cs => cs.SourceType == MediaSourceType.Local);
            return maybeLocalMediaSource.Match<Either<Error, int>>(
                mediaSource => mediaSource.Id,
                () => Error.New("Unable to find local media source"));
        }

        private async Task<Either<Error, List<int>>> SynchronizeMediaItemsAsync(
            int mediaSourceId,
            ICollection<string> fileNames,
            CancellationToken cancellationToken)
        {
            var mediaItemsApi = new MediaItemsApi(_serverUrl);
            List<MediaItemViewModel> allMediaItems = await mediaItemsApi.ApiMediaItemsGetAsync(cancellationToken);
            var missingMediaItems = fileNames.Where(f => allMediaItems.All(c => c.Path != f))
                .Map(f => new CreateMediaItem(mediaSourceId, f))
                .ToList();

            var addedIds = new List<int>();
            foreach (CreateMediaItem mediaItem in missingMediaItems)
            {
                _logger.LogInformation("Adding media item {Path}", mediaItem.Path);
                addedIds.Add(await mediaItemsApi.ApiMediaItemsPostAsync(mediaItem, cancellationToken).Map(vm => vm.Id));
            }

            IEnumerable<int> knownIds = allMediaItems.Where(c => fileNames.Contains(c.Path)).Map(c => c.Id);

            return knownIds.Concat(addedIds).ToList();
        }

        private async Task<Either<Error, Unit>> SynchronizeMediaItemsToCollectionAsync(
            List<int> mediaItemIds,
            CancellationToken cancellationToken) =>
            await EnsureMediaCollectionExistsAsync(cancellationToken)
                .BindAsync(
                    mediaSourceId => SynchronizeMediaCollectionAsync(mediaSourceId, mediaItemIds, cancellationToken));

        private async Task<Either<Error, int>> EnsureMediaCollectionExistsAsync(CancellationToken cancellationToken)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);
            Option<MediaCollectionViewModel> maybeExisting = await mediaCollectionsApi
                .ApiMediaCollectionsGetAsync(cancellationToken)
                .Map(list => list.SingleOrDefault(mc => mc.Name == Name));
            return await maybeExisting.Match(
                existing => Task.FromResult(existing.Id),
                async () =>
                {
                    var data = new CreateSimpleMediaCollection(Name);
                    return await mediaCollectionsApi.ApiMediaCollectionsPostAsync(data, cancellationToken)
                        .Map(vm => vm.Id);
                });
        }

        private async Task<Either<Error, Unit>> SynchronizeMediaCollectionAsync(
            int mediaCollectionId,
            List<int> mediaItemIds,
            CancellationToken cancellationToken)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);
            await mediaCollectionsApi.ApiMediaCollectionsIdItemsPutAsync(
                mediaCollectionId,
                mediaItemIds,
                cancellationToken);
            return unit;
        }
    }
}
