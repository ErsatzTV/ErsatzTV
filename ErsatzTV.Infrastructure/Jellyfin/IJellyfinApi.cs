using ErsatzTV.Infrastructure.Jellyfin.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Jellyfin;

[Headers("Accept: application/json")]
public interface IJellyfinApi
{
    [Get("/System/Info")]
    Task<JellyfinSystemInformationResponse> GetSystemInformation(
        [Header("X-Emby-Token")]
        string apiKey,
        CancellationToken cancellationToken);

    [Get("/Users")]
    Task<List<JellyfinUserResponse>> GetUsers(
        [Header("X-Emby-Token")]
        string apiKey);

    [Get("/Library/VirtualFolders")]
    Task<List<JellyfinLibraryResponse>> GetLibraries(
        [Header("X-Emby-Token")]
        string apiKey);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetMovieLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,OfficialRating,ProviderIds,Chapters",
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
    Task<JellyfinLibraryItemsResponse> GetShowLibraryItemsWithoutPeople(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Series",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0,
        [Query]
        string ids = null);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetShowLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
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
        int limit = 0,
        [Query]
        string ids = null);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetSeasonLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
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
    Task<JellyfinLibraryItemsResponse> GetEpisodeLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,ProviderIds,People,Chapters",
        [Query]
        string includeItemTypes = "Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0,
        [Query]
        string ids = null);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetEpisodeLibraryItemsWithoutPeople(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields = "Path,Genres,Tags,DateCreated,Etag,Overview,ProviderIds,Chapters",
        [Query]
        string includeItemTypes = "Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetCollectionLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "BoxSet",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    Task<JellyfinLibraryItemsResponse> GetCollectionItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "Movie,Series,Season,Episode",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items/{itemId}/PlaybackInfo")]
    Task<JellyfinPlaybackInfoResponse> GetPlaybackInfo(
        [Header("X-Emby-Token")]
        string apiKey,
        string itemId);

    [Get("/Search/Hints")]
    Task<JellyfinSearchHintsResponse> SearchHints(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string searchTerm,
        [Query]
        string includeItemTypes = "Series",
        [Query]
        string parentId = null,
        [Query]
        int limit = 20);
}
