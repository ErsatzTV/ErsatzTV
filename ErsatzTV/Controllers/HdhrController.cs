using ErsatzTV.Application.Channels;
using ErsatzTV.Application.HDHR;
using ErsatzTV.Core.Hdhr;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class HdhrController(IMediator mediator) : ControllerBase
{
    [HttpGet("device.xml")]
    public async Task<IActionResult> DeviceXml()
    {
        Guid uuid = await mediator.Send(new GetHDHRUUID());
        return new OkObjectResult(new DeviceXml(Request.Scheme, Request.Host.ToString(), uuid));
    }

    [HttpGet("discover.json")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> Discover()
    {
        Guid uuid = await mediator.Send(new GetHDHRUUID());
        int tunerCount = await mediator.Send(new GetHDHRTunerCount());
        return new OkObjectResult(new Discover(Request.Scheme, Request.Host.ToString(), tunerCount, uuid));
    }

    [HttpGet("lineup_status.json")]
    public IActionResult LineupStatus() =>
        new OkObjectResult(new LineupStatus());

    [HttpGet("lineup.json")]
    public Task<IActionResult> Lineup() =>
        mediator.Send(new GetChannelLineup(Request.Scheme, Request.Host.ToString())).ToActionResult();
}
