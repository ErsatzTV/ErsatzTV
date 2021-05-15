using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin
{
    [Headers("Accept: application/json")]
    public interface IJellyfinApi
    {
        [Get("/System/Info")]
        public Task<JellyfinSystemInformationResponse> GetSystemInformation(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Users")]
        public Task<List<JellyfinUserResponse>> GetUsers(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Library/VirtualFolders")]
        public Task<List<JellyfinLibraryResponse>> GetLibraries(
            [Header("X-Emby-Token")]
            string apiKey);

        [Get("/Items")]
        public Task<JellyfinLibraryItemsResponse> GetMovieLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string userId,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People",
            [Query]
            string includeItemTypes = "Movie");

        [Get("/Items")]
        public Task<JellyfinLibraryItemsResponse> GetShowLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string userId,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People",
            [Query]
            string includeItemTypes = "Series");

        [Get("/Items")]
        public Task<JellyfinLibraryItemsResponse> GetSeasonLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string userId,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,DateCreated,Etag,Taglines",
            [Query]
            string includeItemTypes = "Season");

        [Get("/Items")]
        public Task<JellyfinLibraryItemsResponse> GetEpisodeLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string userId,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,DateCreated,Etag,Overview",
            [Query]
            string includeItemTypes = "Episode");
    }
}
