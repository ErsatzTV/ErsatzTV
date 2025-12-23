using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class PlayoutController(ChannelWriter<IBackgroundServiceRequest> workerChannel, IMediator mediator) : ControllerBase
{
    [HttpGet("/api/playouts", Name = "GetAllPlayouts")]
    [Tags("Playouts")]
    [EndpointSummary("Get all playouts (paginated)")]
    [EndpointGroupName("general")]
    public async Task<PagedPlayoutsViewModel> GetAllPlayouts(
        [FromQuery] string query = "",
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedPlayouts(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/playouts/block", Name = "GetBlockPlayouts")]
    [Tags("Playouts")]
    [EndpointSummary("Get all block playouts")]
    [EndpointGroupName("general")]
    public async Task<List<PlayoutNameViewModel>> GetBlockPlayouts(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllBlockPlayouts(), cancellationToken);

    [HttpGet("/api/playouts/{id:int}", Name = "GetPlayoutById")]
    [Tags("Playouts")]
    [EndpointSummary("Get playout by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetPlayoutById(int id, CancellationToken cancellationToken)
    {
        Option<PlayoutNameViewModel> result = await mediator.Send(new GetPlayoutById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/playouts/{id:int}/items", Name = "GetPlayoutItems")]
    [Tags("Playouts")]
    [EndpointSummary("Get playout items")]
    [EndpointGroupName("general")]
    public async Task<PagedPlayoutItemsViewModel> GetPlayoutItems(
        int id,
        [FromQuery] bool showFiller = false,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetFuturePlayoutItemsById(id, showFiller, pageNumber, pageSize), cancellationToken);

    [HttpPost("/api/playouts/classic", Name = "CreateClassicPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Create a classic playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateClassicPlayout(
        [Required] [FromBody] CreateClassicPlayoutRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreatePlayoutResponse> result = await mediator.Send(
            new CreateClassicPlayout(request.ChannelId, request.ProgramScheduleId), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPost("/api/playouts/block", Name = "CreateBlockPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Create a block playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateBlockPlayout(
        [Required] [FromBody] CreateBlockPlayoutRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreatePlayoutResponse> result = await mediator.Send(
            new CreateBlockPlayout(request.ChannelId), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPost("/api/playouts/sequential", Name = "CreateSequentialPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Create a sequential playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateSequentialPlayout(
        [Required] [FromBody] CreateSequentialPlayoutRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreatePlayoutResponse> result = await mediator.Send(
            new CreateSequentialPlayout(request.ChannelId, request.ScheduleFile), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPost("/api/playouts/scripted", Name = "CreateScriptedPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Create a scripted playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateScriptedPlayout(
        [Required] [FromBody] CreateScriptedPlayoutRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreatePlayoutResponse> result = await mediator.Send(
            new CreateScriptedPlayout(request.ChannelId, request.ScheduleFile), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPost("/api/playouts/external-json", Name = "CreateExternalJsonPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Create an external JSON playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateExternalJsonPlayout(
        [Required] [FromBody] CreateExternalJsonPlayoutRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreatePlayoutResponse> result = await mediator.Send(
            new CreateExternalJsonPlayout(request.ChannelId, request.ScheduleFile), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/playouts/{id:int}", Name = "DeletePlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Delete a playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeletePlayout(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeletePlayout(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/playouts/{id:int}/build", Name = "BuildPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Build a playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> BuildPlayout(int id)
    {
        await workerChannel.WriteAsync(new BuildPlayout(id, PlayoutBuildMode.Refresh));
        return Accepted();
    }

    [HttpPost("/api/playouts/{id:int}/reset", Name = "ResetPlayout")]
    [Tags("Playouts")]
    [EndpointSummary("Reset a playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> ResetPlayout(int id)
    {
        await workerChannel.WriteAsync(new BuildPlayout(id, PlayoutBuildMode.Reset));
        return Accepted();
    }

    [HttpPost("/api/playouts/reset-all", Name = "ResetAllPlayouts")]
    [Tags("Playouts")]
    [EndpointSummary("Reset all playouts")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> ResetAllPlayouts(CancellationToken cancellationToken)
    {
        await mediator.Send(new ResetAllPlayouts(), cancellationToken);
        return Accepted();
    }

    [HttpGet("/api/playouts/warnings/count", Name = "GetPlayoutWarningsCount")]
    [Tags("Playouts")]
    [EndpointSummary("Get playout warnings count")]
    [EndpointGroupName("general")]
    public async Task<int> GetPlayoutWarningsCount(CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlayoutWarningsCount(), cancellationToken);
}

// Request models
public record CreateClassicPlayoutRequest(int ChannelId, int ProgramScheduleId);
public record CreateBlockPlayoutRequest(int ChannelId);
public record CreateSequentialPlayoutRequest(int ChannelId, string ScheduleFile);
public record CreateScriptedPlayoutRequest(int ChannelId, string ScheduleFile);
public record CreateExternalJsonPlayoutRequest(int ChannelId, string ScheduleFile);
