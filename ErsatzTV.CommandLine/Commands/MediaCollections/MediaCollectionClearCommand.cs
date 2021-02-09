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
    [Command("collection clear", Description = "Removes all items from a media collection")]
    public class MediaCollectionClearCommand : ICommand
    {
        private readonly ILogger<MediaCollectionClearCommand> _logger;
        private readonly string _serverUrl;

        public MediaCollectionClearCommand(IConfiguration configuration, ILogger<MediaCollectionClearCommand> logger)
        {
            _logger = logger;
            _serverUrl = configuration["ServerUrl"];
        }

        [CommandParameter(0, Name = "collection-name", Description = "The name of the media collection")]
        public string Name { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken cancellationToken = console.GetCancellationToken();

                Either<Error, Unit> result = await ClearMediaCollection(cancellationToken);

                result.Match(
                    _ => _logger.LogInformation("Successfully cleared media collection {MediaCollection}", Name),
                    error => _logger.LogError(
                        "Unable to clear media collection: {Error}",
                        error.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to clear media collection: {Error}", ex.Message);
            }
        }

        private async Task<Either<Error, Unit>> ClearMediaCollection(CancellationToken cancellationToken) =>
            await EnsureMediaCollectionExists(cancellationToken)
                .BindAsync(mediaCollectionId => ClearMediaCollectionImpl(mediaCollectionId, cancellationToken));

        private async Task<Either<Error, int>> EnsureMediaCollectionExists(CancellationToken cancellationToken)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);
            Option<MediaCollectionViewModel> maybeExisting =
                (await mediaCollectionsApi.ApiMediaCollectionsGetAsync(cancellationToken))
                .SingleOrDefault(mc => mc.Name == Name);
            return await maybeExisting.MatchAsync(
                existing => existing.Id,
                async () =>
                {
                    var data = new CreateSimpleMediaCollection(Name);
                    MediaCollectionViewModel result =
                        await mediaCollectionsApi.ApiMediaCollectionsPostAsync(data, cancellationToken);
                    return result.Id;
                });
        }

        private async Task<Either<Error, Unit>> ClearMediaCollectionImpl(
            int mediaCollectionId,
            CancellationToken cancellationToken)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);
            await mediaCollectionsApi.ApiMediaCollectionsIdItemsPutAsync(
                mediaCollectionId,
                new List<int>(),
                cancellationToken);
            return unit;
        }
    }
}
