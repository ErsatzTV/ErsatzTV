using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin;

[Headers("Accept: application/json")]
public interface IJellyfinApi
{
    [Get("/System/Info")]
    public Task<JellyfinSystemInformationResponse> GetSystemInformation(
        [Header("X-Emby-Token")]
        string apiKey,
        CancellationToken cancellationToken);

    [Get("/Users")]
    public Task<List<JellyfinUserResponse>> GetUsers(
        [Header("X-Emby-Token")]
        string apiKey);

    [Get("/Library/VirtualFolders")]
    public Task<List<JellyfinLibraryResponse>> GetLibraries(
        [Header("X-Emby-Token")]
        string apiKey);

    [Get("/Items")]
    public Task<JellyfinLibraryItemsResponse> GetLibraryStats(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string includeItemTypes,
        [Query]
        bool recursive = true,
        [Query]
        string filters = "IsNotFolder",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetMovieLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Movie",
        [Query]
        bool recursive = true,
        [Query]
        string filters = "IsNotFolder",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetShowLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Series",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetSeasonLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,DateCreated,Etag,Taglines,ProviderIds",
        [Query]
        string includeItemTypes = "Season",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<JellyfinLibraryItemsResponse> GetEpisodeLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,DateCreated,Etag,Overview,ProviderIds,People",
        [Query]
        string includeItemTypes = "Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items")]
    public Task<JellyfinLibraryItemsResponse> GetCollectionLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "BoxSet",
        [Query]
        bool recursive = true);

    [Get("/Items")]
    public Task<JellyfinLibraryItemsResponse> GetCollectionItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string userId,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "Movie,Series,Season,Episode",
        [Query]
        bool recursive = true);
}
