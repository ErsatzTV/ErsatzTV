using ErsatzTV.Infrastructure.Emby.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Emby;

[Headers("Accept: application/json")]
public interface IEmbyApi
{
    [Get("/System/Info")]
    public Task<EmbySystemInformationResponse> GetSystemInformation(
        [Header("X-Emby-Token")]
        string apiKey,
        CancellationToken cancellationToken);

    [Get("/Library/VirtualFolders")]
    public Task<List<EmbyLibraryResponse>> GetLibraries(
        [Header("X-Emby-Token")]
        string apiKey);

    [Get("/Items")]
    public Task<EmbyLibraryItemsResponse> GetLibraryStats(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string includeItemTypes,
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<EmbyLibraryItemsResponse> GetMovieLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,ProductionYear,PremiereDate,MediaSources,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Movie",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<EmbyLibraryItemsResponse> GetShowLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,Taglines,Studios,People,ProductionYear,PremiereDate,MediaSources,OfficialRating,ProviderIds",
        [Query]
        string includeItemTypes = "Series",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Shows/{parentId}/Seasons?sortOrder=Ascending&sortBy=SortName")]
    public Task<EmbyLibraryItemsResponse> GetSeasonLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        string parentId,
        [Query]
        string fields = "Path,DateCreated,Etag,Taglines,ProviderIds",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Shows/{showId}/Episodes?sortOrder=Ascending&sortBy=SortName")]
    public Task<EmbyLibraryItemsResponse> GetEpisodeLibraryItems(
        [Header("X-Emby-Token")]
        string apiKey,
        string showId,
        [Query]
        string seasonId,
        [Query]
        string fields =
            "Path,Genres,Tags,DateCreated,Etag,Overview,ProductionYear,PremiereDate,MediaSources,LocationType,ProviderIds,People",
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);

    [Get("/Items?sortOrder=Ascending&sortBy=SortName")]
    public Task<EmbyLibraryItemsResponse> GetCollectionLibraryItems(
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
    public Task<EmbyLibraryItemsResponse> GetCollectionItems(
        [Header("X-Emby-Token")]
        string apiKey,
        [Query]
        string parentId,
        [Query]
        string fields = "Etag",
        [Query]
        string includeItemTypes = "Movie,Series,Season,Episode",
        [Query]
        string excludeLocationTypes = "Virtual",
        [Query]
        bool recursive = true,
        [Query]
        int startIndex = 0,
        [Query]
        int limit = 0);
}
