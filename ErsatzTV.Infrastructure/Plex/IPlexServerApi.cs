using System.Threading.Tasks;
using ErsatzTV.Infrastructure.Plex.Models;
using Refit;

namespace ErsatzTV.Infrastructure.Plex;

public interface IPlexServerApi
{
    [Get("/")]
    [Headers("Accept: application/json")]
    public Task Ping(
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/sections")]
    [Headers("Accept: application/json")]
    public Task<PlexMediaContainerResponse<PlexMediaContainerDirectoryContent<PlexLibraryResponse>>> GetLibraries(
        [Query] [AliasAs("X-Plex-Token")]
        string token);

    [Get("/library/sections/{key}/all")]
    [Headers("Accept: application/json")]
    public Task<PlexMediaContainerResponse<PlexMediaContainerMetadataContent<PlexMetadataResponse>>>
        GetLibrarySectionContents(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}")]
    [Headers("Accept: text/xml")]
    public Task<PlexXmlVideoMetadataResponseContainer>
        GetVideoMetadata(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}")]
    [Headers("Accept: text/xml")]
    public Task<PlexXmlDirectoryMetadataResponseContainer>
        GetDirectoryMetadata(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}/children")]
    [Headers("Accept: text/xml")]
    public Task<PlexXmlSeasonsMetadataResponseContainer>
        GetShowChildren(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);

    [Get("/library/metadata/{key}/children")]
    [Headers("Accept: text/xml")]
    public Task<PlexXmlEpisodesMetadataResponseContainer>
        GetSeasonChildren(
            string key,
            [Query] [AliasAs("X-Plex-Token")]
            string token);
}