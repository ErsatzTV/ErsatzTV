using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.Scheduling;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class ScheduleController
{
    private readonly IMediator _mediator;

    public ScheduleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("/api/schedule/blocks")]
    public async Task<List<ScheduleBlockResponseModel>> GetAll() =>
        await _mediator.Send(new GetAllScheduleBlocksForApi());

    [HttpPost("/api/schedule/blocks/new")]
    public async Task<Either<BaseError, CreateScheduleBlockResult>> AddOne(
        [Required] [FromBody]
        CreateScheduleBlock request) => await _mediator.Send(request);
    
    [HttpPut("/api/schedule/blocks/update")]
    public async Task<Either<BaseError, UpdateScheduleBlockResult>> UpdateOne(
        [Required] [FromBody]
        UpdateScheduleBlock request) => await _mediator.Send(request);

    [HttpGet("/api/schedule/blocks/{id}")]
    public async Task<Option<ScheduleBlockResponseModel>> GetOne(int id) =>
        await _mediator.Send(new GetScheduleBlockByIdForApi(id));
}
