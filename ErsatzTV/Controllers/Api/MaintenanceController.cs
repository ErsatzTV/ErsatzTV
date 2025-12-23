using ErsatzTV.Application.Maintenance;
using ErsatzTV.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class MaintenanceController(IMediator mediator) : ControllerBase
{
    [HttpPost("/api/maintenance/gc", Name = "GarbageCollection")]
    [Tags("Maintenance")]
    [EndpointSummary("Garbage collect")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GarbageCollection([FromQuery] bool force = false)
    {
        await mediator.Send(new ReleaseMemory(force));
        return Ok();
    }

    [HttpPost("/api/maintenance/empty-trash", Name = "EmptyTrash")]
    [Tags("Maintenance")]
    [EndpointSummary("Empty trash")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> EmptyTrash()
    {
        Either<BaseError, Unit> result = await mediator.Send(new EmptyTrash());
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/maintenance/delete-orphaned-artwork", Name = "DeleteOrphanedArtwork")]
    [Tags("Maintenance")]
    [EndpointSummary("Delete orphaned artwork")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteOrphanedArtwork()
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteOrphanedArtwork());
        return result.Match<IActionResult>(_ => Accepted(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/maintenance/delete-orphaned-subtitles", Name = "DeleteOrphanedSubtitles")]
    [Tags("Maintenance")]
    [EndpointSummary("Delete orphaned subtitles")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteOrphanedSubtitles()
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteOrphanedSubtitles());
        return result.Match<IActionResult>(_ => Accepted(), error => Problem(error.ToString()));
    }
}
