using ErsatzTV.Application.MediaCards;
using ErsatzTV.Application.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class SearchController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/search", Name = "SearchAll")]
    [Tags("Search")]
    [EndpointSummary("Search all media types")]
    [EndpointGroupName("general")]
    public async Task<SearchResultAllItemsViewModel> SearchAll(
        [FromQuery] string query,
        CancellationToken cancellationToken) =>
        await mediator.Send(new QuerySearchIndexAllItems(query), cancellationToken);

    [HttpGet("/api/search/movies", Name = "SearchMovies")]
    [Tags("Search")]
    [EndpointSummary("Search movies")]
    [EndpointGroupName("general")]
    public async Task<MovieCardResultsViewModel> SearchMovies(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexMovies(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/shows", Name = "SearchShows")]
    [Tags("Search")]
    [EndpointSummary("Search TV shows")]
    [EndpointGroupName("general")]
    public async Task<TelevisionShowCardResultsViewModel> SearchShows(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexShows(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/episodes", Name = "SearchEpisodes")]
    [Tags("Search")]
    [EndpointSummary("Search episodes")]
    [EndpointGroupName("general")]
    public async Task<TelevisionEpisodeCardResultsViewModel> SearchEpisodes(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexEpisodes(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/artists", Name = "SearchArtists")]
    [Tags("Search")]
    [EndpointSummary("Search artists")]
    [EndpointGroupName("general")]
    public async Task<ArtistCardResultsViewModel> SearchArtists(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexArtists(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/music-videos", Name = "SearchMusicVideos")]
    [Tags("Search")]
    [EndpointSummary("Search music videos")]
    [EndpointGroupName("general")]
    public async Task<MusicVideoCardResultsViewModel> SearchMusicVideos(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexMusicVideos(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/songs", Name = "SearchSongs")]
    [Tags("Search")]
    [EndpointSummary("Search songs")]
    [EndpointGroupName("general")]
    public async Task<SongCardResultsViewModel> SearchSongs(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexSongs(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/search/images", Name = "SearchImages")]
    [Tags("Search")]
    [EndpointSummary("Search images")]
    [EndpointGroupName("general")]
    public async Task<ImageCardResultsViewModel> SearchImages(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new QuerySearchIndexImages(query, pageNumber, pageSize), cancellationToken);

    [HttpPost("/api/search/rebuild", Name = "RebuildSearchIndex")]
    [Tags("Search")]
    [EndpointSummary("Rebuild search index")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> RebuildSearchIndex(CancellationToken cancellationToken)
    {
        await mediator.Send(new RebuildSearchIndex(), cancellationToken);
        return Accepted();
    }
}
