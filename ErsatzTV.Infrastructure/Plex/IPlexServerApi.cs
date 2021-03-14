using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Plex.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Plex
{
    [Headers("Accept: application/json")]
    public interface IPlexServerApi
    {
        [Get("/library/sections")]
        public Task<PlexMediaContainerResponse<PlexMediaContainerDirectoryContent<PlexLibraryResponse>>> GetLibraries(
            [Query] [AliasAs("X-Plex-Token")]
            string token);

        [Get("/library/sections/{key}/all")]
        public Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
            GetLibrarySectionContents(
                string key,
                [Query] [AliasAs("X-Plex-Token")]
                string token);

        [Get("/library/metadata/{key}")]
        public Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
            GetMetadata(
                string key,
                [Query] [AliasAs("X-Plex-Token")]
                string token);

        [Get("/library/metadata/{key}/children")]
        public Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
            GetChildren(
                string key,
                [Query] [AliasAs("X-Plex-Token")]
                string token);
    }
}
