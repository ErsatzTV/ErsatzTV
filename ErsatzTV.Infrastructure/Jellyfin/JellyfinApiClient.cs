using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    public class JellyfinApiClient : IJellyfinApiClient
    {
        private readonly ILogger<JellyfinApiClient> _logger;
        private readonly IMemoryCache _memoryCache;

        public JellyfinApiClient(IMemoryCache memoryCache, ILogger<JellyfinApiClient> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<Either<BaseError, string>> GetServerName(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                JellyfinConfigurationResponse config = await service.GetConfiguration(apiKey);
                return config.ServerName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin server name");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<JellyfinLibrary>>> GetLibraries(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                List<JellyfinLibraryResponse> libraries = await service.GetLibraries(apiKey);
                return libraries
                    .Map(Project)
                    .Somes()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin libraries");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, string>> GetAdminUserId(string address, string apiKey)
        {
            try
            {
                IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                List<JellyfinUserResponse> users = await service.GetUsers(apiKey);
                Option<string> maybeUserId = users
                    .Filter(user => user.Policy.IsAdministrator)
                    .Map(user => user.Id)
                    .HeadOrNone();

                return maybeUserId.ToEither(BaseError.New("Unable to locate jellyfin admin user"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin admin user id");
                return BaseError.New(ex.Message);
            }
        }

        public async Task<Either<BaseError, List<string>>> GetLibraryItems(
            string address,
            string apiKey,
            int mediaSourceId,
            string libraryId)
        {
            try
            {
                if (_memoryCache.TryGetValue($"jellyfin_admin_user_id.{mediaSourceId}", out string userId))
                {

                    IJellyfinApi service = RestService.For<IJellyfinApi>(address);
                    JellyfinLibraryItemsResponse items = await service.GetLibraryItems(apiKey, userId, libraryId);

                    return new List<string>();
                }

                return BaseError.New("Jellyfin admin user id is not available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting jellyfin library items");
                return BaseError.New(ex.Message);
            }
        }

        private static Option<JellyfinLibrary> Project(JellyfinLibraryResponse response) =>
            response.CollectionType.ToLowerInvariant() switch
            {
                "tvshows" => new JellyfinLibrary
                {
                    ItemId = response.ItemId,
                    Name = response.Name,
                    MediaKind = LibraryMediaKind.Shows,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } }
                },
                "movies" => new JellyfinLibrary
                {
                    ItemId = response.ItemId,
                    Name = response.Name,
                    MediaKind = LibraryMediaKind.Movies,
                    ShouldSyncItems = false,
                    Paths = new List<LibraryPath> { new() { Path = $"jellyfin://{response.ItemId}" } }
                },
                // TODO: ??? for music libraries
                _ => None
            };
    }
}
