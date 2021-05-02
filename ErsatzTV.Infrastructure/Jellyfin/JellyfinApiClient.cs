using System;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Jellyfin;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    public class JellyfinApiClient : IJellyfinApiClient
    {
        private readonly ILogger<JellyfinApiClient> _logger;

        public JellyfinApiClient(ILogger<JellyfinApiClient> logger) => _logger = logger;

        public async Task<Either<BaseError, string>> GetServerName(JellyfinSecrets secrets)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(secrets.Address);
                JellyfinConfigurationResponse config = await service.GetConfiguration(secrets.ApiKey);
                return config.ServerName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin server name");
                return BaseError.New(ex.Message);
            }
        }
    }
}
