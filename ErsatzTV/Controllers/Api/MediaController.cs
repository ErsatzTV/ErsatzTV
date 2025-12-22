using ErsatzTV.Application.Artists;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Application.Movies;
using ErsatzTV.Application.Television;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class MediaController(IMediator mediator) : ControllerBase
{
    // Movies
    [HttpGet("/api/media/movies/{id:int}", Name = "GetMovieById")]
    [Tags("Media")]
    [EndpointSummary("Get movie by ID")]
    public async Task<IActionResult> GetMovieById(int id, CancellationToken cancellationToken)
    {
        Option<MovieViewModel> result = await mediator.Send(new GetMovieById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    // TV Shows
    [HttpGet("/api/media/shows/{id:int}", Name = "GetShowById")]
    [Tags("Media")]
    [EndpointSummary("Get TV show by ID")]
    public async Task<IActionResult> GetShowById(int id, CancellationToken cancellationToken)
    {
        Option<TelevisionShowViewModel> result = await mediator.Send(new GetTelevisionShowById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/media/shows/{id:int}/seasons", Name = "GetShowSeasons")]
    [Tags("Media")]
    [EndpointSummary("Get seasons for a TV show")]
    public async Task<TelevisionSeasonCardResultsViewModel> GetShowSeasons(
        int id,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetTelevisionSeasonCards(id, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/media/seasons/{id:int}", Name = "GetSeasonById")]
    [Tags("Media")]
    [EndpointSummary("Get season by ID")]
    public async Task<IActionResult> GetSeasonById(int id, CancellationToken cancellationToken)
    {
        Option<TelevisionSeasonViewModel> result = await mediator.Send(new GetTelevisionSeasonById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/media/seasons/{id:int}/episodes", Name = "GetSeasonEpisodes")]
    [Tags("Media")]
    [EndpointSummary("Get episodes for a season")]
    public async Task<TelevisionEpisodeCardResultsViewModel> GetSeasonEpisodes(
        int id,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetTelevisionEpisodeCards(id, pageNumber, pageSize), cancellationToken);

    // Artists
    [HttpGet("/api/media/artists/{id:int}", Name = "GetArtistById")]
    [Tags("Media")]
    [EndpointSummary("Get artist by ID")]
    public async Task<IActionResult> GetArtistById(int id, CancellationToken cancellationToken)
    {
        Option<ArtistViewModel> result = await mediator.Send(new GetArtistById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }
}
