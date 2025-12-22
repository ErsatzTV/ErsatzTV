using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class TraktController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/trakt/lists", Name = "GetTraktLists")]
    [Tags("Trakt")]
    [EndpointSummary("Get all Trakt lists (paginated)")]
    public async Task<PagedTraktListsViewModel> GetTraktLists(
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedTraktLists(pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/trakt/lists/{id:int}", Name = "GetTraktListById")]
    [Tags("Trakt")]
    [EndpointSummary("Get Trakt list by ID")]
    public async Task<IActionResult> GetTraktListById(int id, CancellationToken cancellationToken)
    {
        Option<TraktListViewModel> result = await mediator.Send(new GetTraktListById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPut("/api/trakt/lists/{id:int}", Name = "UpdateTraktList")]
    [Tags("Trakt")]
    [EndpointSummary("Update a Trakt list")]
    public async Task<IActionResult> UpdateTraktList(
        int id,
        [Required] [FromBody] UpdateTraktListRequest request,
        CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(
            new UpdateTraktList(id, request.AutoRefresh, request.GeneratePlaylist), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => Ok());
    }

    [HttpDelete("/api/trakt/lists/{id:int}", Name = "DeleteTraktList")]
    [Tags("Trakt")]
    [EndpointSummary("Delete a Trakt list")]
    public async Task<IActionResult> DeleteTraktList(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteTraktList(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/trakt/lists/{id:int}/match", Name = "MatchTraktListItems")]
    [Tags("Trakt")]
    [EndpointSummary("Match Trakt list items")]
    public async Task<IActionResult> MatchTraktListItems(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new MatchTraktListItems(id), cancellationToken);
        return result.Match<IActionResult>(_ => Accepted(), error => Problem(error.ToString()));
    }
}

// Request models
public record UpdateTraktListRequest(bool AutoRefresh, bool GeneratePlaylist);
