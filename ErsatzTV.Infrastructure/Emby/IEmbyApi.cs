using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Emby.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Emby
{
    [Headers("Accept: application/json")]
    public interface IEmbyApi
    {
        [Get("/System/Info")]
        public Task<EmbySystemInformationResponse> GetSystemInformation(
            [Header("X-Emby-Token")]
            string apiKey);

        // [Get("/Users")]
        // public Task<List<EmbyUserResponse>> GetUsers(
        //     [Header("X-Emby-Token")]
        //     string apiKey);
        //
        [Get("/Library/VirtualFolders")]
        public Task<List<EmbyLibraryResponse>> GetLibraries(
            [Header("X-Emby-Token")]
            string apiKey);
        
        [Get("/Items")]
        public Task<EmbyLibraryItemsResponse> GetMovieLibraryItems(
            [Header("X-Emby-Token")]
            string apiKey,
            [Query]
            string parentId,
            [Query]
            string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,ProductionYear,PremiereDate,MediaSources",
            [Query]
            string includeItemTypes = "Movie");
        
        // [Get("/Items")]
        // public Task<EmbyLibraryItemsResponse> GetShowLibraryItems(
        //     [Header("X-Emby-Token")]
        //     string apiKey,
        //     [Query]
        //     string userId,
        //     [Query]
        //     string parentId,
        //     [Query]
        //     string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People",
        //     [Query]
        //     string includeItemTypes = "Series");
        //
        // [Get("/Items")]
        // public Task<EmbyLibraryItemsResponse> GetSeasonLibraryItems(
        //     [Header("X-Emby-Token")]
        //     string apiKey,
        //     [Query]
        //     string userId,
        //     [Query]
        //     string parentId,
        //     [Query]
        //     string fields = "Path,DateCreated,Etag,Taglines",
        //     [Query]
        //     string includeItemTypes = "Season");
        //
        // [Get("/Items")]
        // public Task<EmbyLibraryItemsResponse> GetEpisodeLibraryItems(
        //     [Header("X-Emby-Token")]
        //     string apiKey,
        //     [Query]
        //     string userId,
        //     [Query]
        //     string parentId,
        //     [Query]
        //     string fields = "Path,DateCreated,Etag,Overview",
        //     [Query]
        //     string includeItemTypes = "Episode");
    }
}
