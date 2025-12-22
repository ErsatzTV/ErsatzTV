using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.ProgramSchedules;
using ErsatzTV.Core;
using ErsatzTV.Core.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class ScheduleController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/schedules", Name = "GetAllSchedules")]
    [Tags("Schedules")]
    [EndpointSummary("Get all schedules (paginated)")]
    public async Task<PagedProgramSchedulesViewModel> GetAllSchedules(
        [FromQuery] string query = "",
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedProgramSchedules(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/schedules/all", Name = "GetAllSchedulesList")]
    [Tags("Schedules")]
    [EndpointSummary("Get all schedules")]
    public async Task<List<ProgramScheduleViewModel>> GetAllSchedulesList(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllProgramSchedules(), cancellationToken);

    [HttpGet("/api/schedules/{id:int}", Name = "GetScheduleById")]
    [Tags("Schedules")]
    [EndpointSummary("Get schedule by ID")]
    public async Task<IActionResult> GetScheduleById(int id, CancellationToken cancellationToken)
    {
        Option<ProgramScheduleViewModel> result = await mediator.Send(new GetProgramScheduleById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/schedules/{id:int}/items", Name = "GetScheduleItems")]
    [Tags("Schedules")]
    [EndpointSummary("Get schedule items")]
    public async Task<List<ProgramScheduleItemViewModel>> GetScheduleItems(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetProgramScheduleItems(id), cancellationToken);

    [HttpPost("/api/schedules", Name = "CreateSchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Create a schedule")]
    public async Task<IActionResult> CreateSchedule(
        [Required] [FromBody] CreateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreateProgramScheduleResult> result = await mediator.Send(
            new CreateProgramSchedule(
                request.Name,
                request.KeepMultiPartEpisodesTogether,
                request.TreatCollectionsAsShows,
                request.ShuffleScheduleItems,
                request.RandomStartPoint,
                request.FixedStartTimeBehavior),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/schedules/{id:int}", Name = "DeleteSchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Delete a schedule")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteProgramSchedule(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/schedules/{id:int}/copy", Name = "CopySchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Copy a schedule")]
    public async Task<IActionResult> CopySchedule(
        int id,
        [Required] [FromBody] CopyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, ProgramScheduleViewModel> result = await mediator.Send(
            new CopyProgramSchedule(id, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }
}

// Request models
public record CreateScheduleRequest(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems,
    bool RandomStartPoint,
    FixedStartTimeBehavior FixedStartTimeBehavior);

public record CopyScheduleRequest(string Name);
