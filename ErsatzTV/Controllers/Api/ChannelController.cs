﻿using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Scheduling;
using ErsatzTV.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class ChannelController(ChannelWriter<IBackgroundServiceRequest> workerChannel, IMediator mediator)
{
    [HttpGet("/api/channels")]
    [V2ApiActionFilter]
    public async Task<List<ChannelResponseModel>> GetAll() => await mediator.Send(new GetAllChannelsForApi());

    [HttpPost("/api/channels/{channelNumber}/playout/reset")]
    public async Task<IActionResult> ResetPlayout(string channelNumber)
    {
        Option<int> maybePlayoutId = await mediator.Send(new GetPlayoutIdByChannelNumber(channelNumber));
        foreach (int playoutId in maybePlayoutId)
        {
            await workerChannel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Reset));
            return new OkResult();
        }

        return new NotFoundResult();
    }
}
