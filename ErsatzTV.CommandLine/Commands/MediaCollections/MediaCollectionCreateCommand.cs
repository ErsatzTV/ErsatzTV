using System;
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
    [Command("collection create", Description = "Creates a new media collection")]
    public class MediaCollectionCreateCommand : ICommand
    {
        private readonly ILogger<MediaCollectionCreateCommand> _logger;
        private readonly string _serverUrl;

        public MediaCollectionCreateCommand(IConfiguration configuration, ILogger<MediaCollectionCreateCommand> logger)
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

                Either<Error, Unit> result = await CreateMediaCollection(cancellationToken);
                result.IfLeft(error => _logger.LogError("Unable to create media collection: {Error}", error.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to create media collection: {Error}", ex.Message);
            }
        }

        private async Task<Either<Error, Unit>> CreateMediaCollection(CancellationToken cancellationToken) =>
            await EnsureMediaCollectionExists(cancellationToken);

        private async Task<Either<Error, Unit>> EnsureMediaCollectionExists(CancellationToken cancellationToken)
        {
            var mediaCollectionsApi = new MediaCollectionsApi(_serverUrl);

            bool needToAdd = await mediaCollectionsApi
                .ApiMediaCollectionsGetAsync(cancellationToken)
                .Map(list => list.All(mc => mc.Name != Name));

            if (needToAdd)
            {
                var data = new CreateSimpleMediaCollection(Name);
                await mediaCollectionsApi.ApiMediaCollectionsPostAsync(data, cancellationToken);
                _logger.LogInformation("Successfully created media collection {MediaCollection}", Name);
            }
            else
            {
                _logger.LogInformation("Media collection {MediaCollection} is already present", Name);
            }

            return unit;
        }
    }
}
