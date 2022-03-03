﻿using ErsatzTV.Application.Channels;
using ErsatzTV.Application.HDHR;
using ErsatzTV.Core.Hdhr;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class HdhrController : ControllerBase
{
    private readonly IMediator _mediator;

    public HdhrController(IMediator mediator) => _mediator = mediator;

    [HttpGet("device.xml")]
    public IActionResult DeviceXml() =>
        new OkObjectResult(new DeviceXml(Request.Scheme, Request.Host.ToString()));

    [HttpGet("discover.json")]
    [ResponseCache(NoStore = true)]
    public Task<IActionResult> Discover() =>
        _mediator.Send(new GetHDHRTunerCount()).Map<int, IActionResult>(
            tunerCount => new OkObjectResult(new Discover(Request.Scheme, Request.Host.ToString(), tunerCount)));

    [HttpGet("lineup_status.json")]
    public IActionResult LineupStatus() =>
        new OkObjectResult(new LineupStatus());

    [HttpGet("lineup.json")]
    public Task<IActionResult> Lineup() =>
        _mediator.Send(new GetChannelLineup(Request.Scheme, Request.Host.ToString())).ToActionResult();
}