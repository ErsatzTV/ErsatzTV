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

namespace ErsatzTV.CommandLine.Commands
{
    [Command("items", Description = "Ensure media items exist")]
    public class MediaItemsCommand : MediaItemCommandBase
    {
        private readonly ILogger<MediaItemsCommand> _logger;
        private readonly string _serverUrl;

        public MediaItemsCommand(IConfiguration configuration, ILogger<MediaItemsCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        public override async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                Either<Error, List<string>> maybeFileNames = await GetFileNames();
                await maybeFileNames.Match(
                    allFiles => SynchronizeMediaItems(cancellationToken, allFiles),
                    error =>
                    {
                        _logger.LogError("{Error}", error.Message);
                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to synchronize media items: {Error}", ex.Message);
            }
        }

        private async Task SynchronizeMediaItems(CancellationToken cancellationToken, List<string> allFiles)
        {
            Either<Error, Unit> result = await GetMediaSourceId(cancellationToken)
                .BindAsync(
                    contentSourceId => PostMediaItems(
                        contentSourceId,
                        allFiles,
                        cancellationToken));

            result.Match(
                _ => _logger.LogInformation(
                    "Successfully synchronized {Count} media items",
                    allFiles.Count),
                error => _logger.LogError("Unable to synchronize media items: {Error}", error.Message));
        }

        private async Task<Either<Error, int>> GetMediaSourceId(CancellationToken cancellationToken)
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

        private async Task<Either<Error, Unit>> PostMediaItems(
            int mediaSourceId,
            ICollection<string> fileNames,
            CancellationToken cancellationToken)
        {
            var mediaItemsApi = new MediaItemsApi(_serverUrl);
            List<MediaItemViewModel> allContent = await mediaItemsApi.ApiMediaItemsGetAsync(cancellationToken);
            var missingMediaItems = fileNames.Where(f => allContent.All(c => c.Path != f))
                .Map(f => new CreateMediaItem(mediaSourceId, f))
                .ToList();

            foreach (CreateMediaItem mediaItem in missingMediaItems)
            {
                _logger.LogInformation("Adding media item {Path}", mediaItem.Path);
                await mediaItemsApi.ApiMediaItemsPostAsync(mediaItem, cancellationToken);
            }

            return unit;
        }
    }
}
