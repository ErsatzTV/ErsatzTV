using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    [Headers("Accept: application/json")]
    public interface IJellyfinApi
    {
        [Get("/System/Configuration")]
        public Task<JellyfinConfigurationResponse> GetConfiguration(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Library/VirtualFolders")]
        public Task<List<JellyfinLibraryResponse>> GetLibraries(
            [Header("X-Emby-Token")]
            string apiKey);
    }
}
