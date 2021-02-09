using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Plex.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Plex
{
    [Headers("Accept: application/json")]
    public interface IPlexServerApi
    {
        [Get("/library/sections")]
        public Task<PlexMediaContainerResponse<PlexLibraryResponse>> GetLibraries(
            [Query] [AliasAs("X-Plex-Token")]
            string token);
    }
}
