using System.Threading.Channels;
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
    [EndpointGroupName("general")]
    public async Task<List<ChannelResponseModel>> GetAll() => await mediator.Send(new GetAllChannelsForApi());

    [HttpPost("/api/channels/{channelNumber}/playout/reset")]
    [Tags("Channels")]
    [EndpointSummary("Reset channel playout")]
    [EndpointGroupName("general")]
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

    // for debugging by fast-forwarding a playout
    // [HttpPost("/api/channels/{channelNumber}/playout/continue")]
    // public async Task<IActionResult> ContinuePlayout(string channelNumber, [FromQuery] int days = 1)
    // {
    //     Option<int> maybePlayoutId = await mediator.Send(new GetPlayoutIdByChannelNumber(channelNumber));
    //     foreach (int playoutId in maybePlayoutId)
    //     {
    //         DateTimeOffset start = DateTimeOffset.Now;
    //         for (int i = 0; i < 24 * days; i++)
    //         {
    //             await workerChannel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Continue, start));
    //             start += TimeSpan.FromHours(1);
    //         }
    //
    //         return new OkResult();
    //     }
    //
    //     return new NotFoundResult();
    // }
}
