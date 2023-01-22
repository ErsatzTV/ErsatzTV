using ErsatzTV.Application.Maintenance;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class MaintenanceController
{
    private readonly IMediator _mediator;

    public MaintenanceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("/api/maintenance/gc")]
    public async Task<IActionResult> GarbageCollection([FromQuery] bool force = false)
    {
        await _mediator.Send(new ReleaseMemory(force));
        return new OkResult();
    }
}
