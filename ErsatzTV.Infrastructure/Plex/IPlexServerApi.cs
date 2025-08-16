using ErsatzTV.Infrastructure.Plex.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Plex;

public interface IPlexServerApi
{
    [Get("/")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerPingResponse> Ping(
        [Query] [AliasAs("X-Plex-Token")]
        string token,
        CancellationToken cancellationToken);

    [Get("/library/sections")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerDirectoryContent<PlexLibraryResponse>>> GetLibraries(
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/sections/{key}/all?X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> GetLibrarySection(
        string key,
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/sections/{key}/all")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
        GetLibrarySectionContents(
            string key,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/all?type=18&X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> GetCollectionCount(
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/all?type=18")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexCollectionMetadataResponse>>>
        GetCollections(
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/collections/{key}/children?X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> GetCollectionItemsCount(
        string key,
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/collections/{key}/children")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexCollectionItemMetadataResponse>>>
        GetCollectionItems(
            string key,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/tags?type={type}&X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> GetTagsCount(
        int type,
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/tags?type={type}")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerDirectoryContent<PlexTagMetadataResponse>>>
        GetTags(
            int type,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/sections/{key}/all?X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> CountTagContents(
        string key,
        [Query] [AliasAs("X-Plex-Token")]
        string token,
        [Query]
        NetworkFilter filter);

    [Get("/library/sections/{key}/all")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
        GetTagContents(
            string key,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token,
            [Query]
            NetworkFilter filter);

    [Get("/library/metadata/{key}?includeChapters=1")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlVideoMetadataResponseContainer>
        GetVideoMetadata(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlDirectoryMetadataResponseContainer>
        GetDirectoryMetadata(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}/children?X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> CountShowChildren(
        string key,
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/metadata/{key}/children")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlSeasonsMetadataResponseContainer>
        GetShowChildren(
            string key,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}/children?X-Plex-Container-Start=0&X-Plex-Container-Size=0")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlMediaContainerStatsResponse> CountSeasonChildren(
        string key,
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/metadata/{key}/children")]
    [Headers("Accept: text/xml")]
    Task<PlexXmlEpisodesMetadataResponseContainer>
        GetSeasonChildren(
            string key,
            [Query] [AliasAs("X-Plex-Container-Start")]
            int skip,
            [Query] [AliasAs("X-Plex-Container-Size")]
            int take,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/hubs/search")]
    [Headers("Accept: application/json")]
    Task<PlexMediaContainerResponse<PlexMediaContainerHubContent<PlexHubResponse>>>
        Search(
            [Query] [AliasAs("query")]
            string searchTerm,
            [Query] [AliasAs("sectionId")]
            string sectionId,
            [Query] [AliasAs("X-Plex-Token")]
            string token);
}
