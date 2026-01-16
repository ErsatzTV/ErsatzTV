using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Maintenance;
using ErsatzTV.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class MaintenanceController(IMediator mediator, ChannelWriter<IBackgroundServiceRequest> workerChannel)
{
    [HttpGet("/api/maintenance/gc")]
    [Tags("Maintenance")]
    [EndpointSummary("Garbage collect")]
    public async Task<IActionResult> GarbageCollection([FromQuery] bool force = false)
    {
        await mediator.Send(new ReleaseMemory(force));
        return new OkResult();
    }

    [HttpPost("/api/maintenance/empty_trash")]
    [Tags("Maintenance")]
    [EndpointSummary("Empty trash")]
    public async Task<IActionResult> EmptyTrash()
    {
        Either<BaseError, Unit> result = await mediator.Send(new EmptyTrash());
        foreach (BaseError error in result.LeftToSeq())
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = error.ToString(),
                ContentType = "text/plain"
            };
        }

        return new OkResult();
    }

    [HttpPost("/api/maintenance/clean_artwork")]
    [Tags("Maintenance")]
    [EndpointSummary("Clean artwork cache")]
    public async Task<IActionResult> CleanArtwork(CancellationToken cancellationToken)
    {
        await workerChannel.WriteAsync(new DeleteOrphanedArtwork(), cancellationToken);
        return new OkResult();
    }
}
